using Microsoft.Extensions.DependencyInjection;
using ShadowFox.Core.Services;

namespace ShadowFox.Core.Configuration;

/// <summary>
/// Extension methods for configuring services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ShadowFox Core services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddShadowFoxCore(this IServiceCollection services)
    {
        // Register core services
        services.AddSingleton<FingerprintGenerator>();
        
        // Add other core services as they are implemented
        // services.AddScoped<IProfileService, ProfileService>();
        // services.AddScoped<IGroupService, GroupService>();
        // services.AddScoped<IFingerprintService, FingerprintService>();

        return services;
    }
}