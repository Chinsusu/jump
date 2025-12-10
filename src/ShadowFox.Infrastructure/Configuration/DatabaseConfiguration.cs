using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShadowFox.Infrastructure.Data;
using ShadowFox.Infrastructure.Services;

namespace ShadowFox.Infrastructure.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString, string encryptionKey)
    {
        // Register encryption service
        services.AddSingleton<IEncryptionService>(provider => new EncryptionService(encryptionKey));
        
        // Register DbContext with SQLite
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
            });
            
            // Enable sensitive data logging in development
            #if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            #endif
        });

        return services;
    }

    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Perform integrity checks and ensure database is ready
        var integrityCheckPassed = await context.PerformIntegrityCheckAsync();
        
        if (!integrityCheckPassed)
        {
            throw new InvalidOperationException("Database integrity check failed during startup");
        }
    }
}