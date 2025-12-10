using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShadowFox.Core.Repositories;
using ShadowFox.Core.Services;
using ShadowFox.Infrastructure.Data;
using ShadowFox.Infrastructure.Repositories;
using ShadowFox.Infrastructure.Services;

namespace ShadowFox.Infrastructure.Configuration;

/// <summary>
/// Configuration for integrating all services with dependency injection container
/// Provides optimized setup for production use with caching and connection pooling
/// </summary>
public static class IntegrationConfiguration
{
    /// <summary>
    /// Configures all services for the profile management system with performance optimizations
    /// </summary>
    public static IServiceCollection AddProfileManagementServices(
        this IServiceCollection services, 
        string connectionString,
        string encryptionKey,
        bool enableCaching = true,
        bool enableConnectionPooling = true)
    {
        // Configure Entity Framework with performance optimizations
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            
            if (enableConnectionPooling)
            {
                // Enable connection pooling for better performance
                options.EnableServiceProviderCaching();
                options.EnableSensitiveDataLogging(false); // Disable in production
            }
            
            // Configure query optimization
            options.ConfigureWarnings(warnings =>
            {
                warnings.Default(WarningBehavior.Log);
            });
        });

        // Register encryption service
        services.AddSingleton<IEncryptionService>(provider => 
            new EncryptionService(encryptionKey));

        // Register repositories
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IUsageSessionRepository, UsageSessionRepository>();

        // Register core services
        services.AddScoped<FingerprintGenerator>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IUsageTrackingService, UsageTrackingService>();

        // Add caching if enabled
        if (enableCaching)
        {
            services.AddMemoryCache();
            
            // Register ProfileService with caching decorator
            services.AddScoped<ProfileService>();
            services.AddScoped<IProfileService>(provider =>
            {
                var profileRepository = provider.GetRequiredService<IProfileRepository>();
                var fingerprintGenerator = provider.GetRequiredService<FingerprintGenerator>();
                var innerService = new ProfileService(profileRepository, fingerprintGenerator);
                var cache = provider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                var logger = provider.GetRequiredService<ILogger<CachedProfileService>>();
                return new CachedProfileService(innerService, cache, logger);
            });
        }
        else
        {
            services.AddScoped<IProfileService, ProfileService>();
        }

        // Add performance monitoring
        services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
        });

        return services;
    }

    /// <summary>
    /// Configures services for testing with in-memory database
    /// </summary>
    public static IServiceCollection AddProfileManagementServicesForTesting(
        this IServiceCollection services,
        string? databaseName = null)
    {
        var dbName = databaseName ?? Guid.NewGuid().ToString();
        
        // Configure in-memory database for testing
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase(dbName);
            options.EnableSensitiveDataLogging();
            options.ConfigureWarnings(warnings =>
            {
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning);
            });
        });

        // Register test encryption service
        services.AddSingleton<IEncryptionService>(provider => 
            new EncryptionService("test-encryption-key"));

        // Register repositories
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IUsageSessionRepository, UsageSessionRepository>();

        // Register core services
        services.AddScoped<FingerprintGenerator>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IUsageTrackingService, UsageTrackingService>();

        // Add memory cache for testing
        services.AddMemoryCache();

        // Add logging for testing
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        return services;
    }

    /// <summary>
    /// Ensures database is created and migrations are applied
    /// </summary>
    public static async Task EnsureDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Apply any pending migrations
        if (context.Database.GetPendingMigrations().Any())
        {
            await context.Database.MigrateAsync();
        }
        
        // Perform integrity check
        await context.PerformIntegrityCheckAsync();
    }
}