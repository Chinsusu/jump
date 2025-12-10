using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShadowFox.Core.Repositories;
using ShadowFox.Core.Services;
using ShadowFox.Infrastructure.Data;
using ShadowFox.Infrastructure.Repositories;
using ShadowFox.Infrastructure.Services;

namespace ShadowFox.Infrastructure.Configuration;

/// <summary>
/// Extension methods for configuring Infrastructure services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ShadowFox Infrastructure services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddShadowFoxInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Register database context
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register repositories
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IUsageSessionRepository, UsageSessionRepository>();

        // Register infrastructure services
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IGroupService, GroupService>();

        return services;
    }
}