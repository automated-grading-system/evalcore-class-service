using Class.Application.Abstractions;
using Class.Application.Common;
using Class.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Class.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<ILabService, LabService>();
        return services;
    }
}
