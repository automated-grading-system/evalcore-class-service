using Class.Domain.Entities;
using Class.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Class.Infrastructure.Seed;

public sealed class DatabaseSeeder
{
    private static readonly Guid DefaultLecturerId = Guid.Parse("00000000-0000-0000-0000-000000000102");

    private readonly IConfiguration _configuration;
    private readonly ClassDbContext _dbContext;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IConfiguration configuration,
        ClassDbContext dbContext,
        ILogger<DatabaseSeeder> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedDevelopmentAsync(CancellationToken cancellationToken = default)
    {
        var className = "PRN232 - ASP.NET Core Web API";
        var exists = await _dbContext.Classes.AnyAsync(x => x.Name == className, cancellationToken);
        if (exists)
        {
            return;
        }

        var lecturerId = ReadGuid("DEMO_LECTURER_ID", "Demo:LecturerId", DefaultLecturerId);
        var now = DateTimeOffset.UtcNow;

        await _dbContext.Classes.AddAsync(new ClassroomClass
        {
            Id = Guid.NewGuid(),
            Name = className,
            Description = "Demo class for automated grading system",
            CreatedBy = lecturerId,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded development demo class {ClassName} for lecturer {LecturerId}.", className, lecturerId);
    }

    private Guid ReadGuid(string environmentKey, string configKey, Guid fallback)
    {
        var value = _configuration[environmentKey] ?? _configuration[configKey];
        return Guid.TryParse(value, out var guid) ? guid : fallback;
    }
}
