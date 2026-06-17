namespace Class.Infrastructure.Migrations;

/// <summary>
/// Resolves whether EF Core migrations should be auto-applied on startup.
/// Exposed as a standalone testable class (no WebApplication dependency).
/// </summary>
public static class MigrationOptions
{
    /// <summary>
    /// Determines whether migrations should be applied.
    /// </summary>
    /// <param name="environment">The current ASPNETCORE_ENVIRONMENT value (e.g. "Development").</param>
    /// <param name="rawValue">
    ///     The raw AUTO_APPLY_MIGRATIONS value from env/config.
    ///     Null or whitespace means "use environment default".
    /// </param>
    public static bool Resolve(string environment, string? rawValue)
    {
        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Equals(rawValue.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }

        // Default: true in Development, false otherwise
        return environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
    }
}
