using Class.Infrastructure.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Class.Infrastructure.Persistence;

public sealed class ClassDbContextFactory : IDesignTimeDbContextFactory<ClassDbContext>
{
    public ClassDbContext CreateDbContext(string[] args)
    {
        EnvFileLoader.LoadNearest();

        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5432;Database=ags;Username=ags;Password=ags_password";

        var builder = new DbContextOptionsBuilder<ClassDbContext>();
        builder.UseNpgsql(connectionString);

        return new ClassDbContext(builder.Options);
    }
}
