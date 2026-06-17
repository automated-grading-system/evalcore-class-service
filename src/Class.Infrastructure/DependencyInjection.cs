using Class.Application.Abstractions;
using Class.Infrastructure.Configuration;
using Class.Infrastructure.Messaging;
using Class.Infrastructure.Persistence;
using Class.Infrastructure.Repositories;
using Class.Infrastructure.Seed;
using Class.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Class.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["DATABASE_URL"]
            ?? configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=ags;Username=ags;Password=ags_password";

        var s3Options = S3Options.FromConfiguration(configuration);
        var rabbitMqOptions = RabbitMqOptions.FromConfiguration(configuration);

        services.AddSingleton(s3Options);
        services.AddSingleton(rabbitMqOptions);

        services.AddDbContext<ClassDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IClassRepository, ClassRepository>();
        services.AddScoped<ILabRepository, LabRepository>();
        services.AddScoped<IOutboxEventRepository, OutboxEventRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IStoragePresignService, S3PresignService>();
        services.AddScoped<DatabaseSeeder>();
        services.AddHostedService<OutboxPublisherService>();

        return services;
    }
}
