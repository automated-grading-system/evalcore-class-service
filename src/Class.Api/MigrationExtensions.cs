using Class.Infrastructure.Migrations;
using Class.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Class.Api;

internal static class MigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations on startup using runtime APIs (no dotnet-ef CLI required).
    /// Controlled by AUTO_APPLY_MIGRATIONS env variable.
    /// Defaults to true in Development, false in Production.
    /// </summary>
    internal static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        var raw = Environment.GetEnvironmentVariable("AUTO_APPLY_MIGRATIONS")
                  ?? app.Configuration["AUTO_APPLY_MIGRATIONS"];

        if (!MigrationOptions.Resolve(app.Environment.EnvironmentName, raw))
        {
            app.Logger.LogInformation("Database auto-migration skipped (AUTO_APPLY_MIGRATIONS=false).");
            return;
        }

        const int maxAttempts = 5;
        const int delaySeconds = 2;

        app.Logger.LogInformation("Database migration started.");

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ClassDbContext>();
                await db.Database.MigrateAsync();

                app.Logger.LogInformation("Database migration completed successfully.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransient(ex))
            {
                app.Logger.LogWarning(
                    ex,
                    "Database migration attempt {Attempt}/{Max} failed (transient). Retrying in {Delay}s...",
                    attempt,
                    maxAttempts,
                    delaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
            catch (Exception ex)
            {
                app.Logger.LogCritical(
                    ex,
                    "Database migration failed on attempt {Attempt}/{Max}. Aborting startup.",
                    attempt,
                    maxAttempts);
                throw;
            }
        }
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is TimeoutException
            || ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("refused", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || (ex.InnerException is not null && IsTransient(ex.InnerException));
    }
}
