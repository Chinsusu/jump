using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Models;
using ShadowFox.Infrastructure.Services;

namespace ShadowFox.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly IEncryptionService? _encryptionService;

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Group> Groups => Set<Group>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, IEncryptionService encryptionService) : base(options)
    {
        _encryptionService = encryptionService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Profile configuration
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Tags).HasMaxLength(500);
            entity.Property(p => p.Notes).HasMaxLength(1000);
            entity.Property(p => p.FingerprintJson).HasMaxLength(4000).IsRequired();
            
            // Configure relationship with Group
            entity.HasOne(p => p.Group)
                  .WithMany()
                  .HasForeignKey(p => p.GroupId)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            // Indexes for performance
            entity.HasIndex(p => p.Name).IsUnique();
            entity.HasIndex(p => p.GroupId);
            entity.HasIndex(p => p.CreatedAt);
            entity.HasIndex(p => p.LastOpenedAt);
            entity.HasIndex(p => p.LastModifiedAt);
            entity.HasIndex(p => p.UsageCount);
            entity.HasIndex(p => p.IsActive);

            // Configure encryption for sensitive data
            if (_encryptionService != null)
            {
                entity.Property(p => p.FingerprintJson)
                    .HasConversion(
                        v => _encryptionService.Encrypt(v),
                        v => _encryptionService.Decrypt(v));
            }
        });

        // Group configuration
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).HasMaxLength(200).IsRequired();
            entity.Property(g => g.Description).HasMaxLength(500);
            entity.HasIndex(g => g.Name).IsUnique();
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps before saving
        var entries = ChangeTracker.Entries<Profile>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.LastModifiedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Performs database integrity checks on startup
    /// </summary>
    public async Task<bool> PerformIntegrityCheckAsync()
    {
        try
        {
            // Check if database can be accessed
            await Database.CanConnectAsync();
            
            // Ensure database is created
            await Database.EnsureCreatedAsync();
            
            // Perform basic data integrity checks
            var profileCount = await Profiles.CountAsync();
            var groupCount = await Groups.CountAsync();
            
            // Check for orphaned profiles (profiles with non-existent group IDs)
            var orphanedProfiles = await Profiles
                .Where(p => p.GroupId.HasValue && !Groups.Any(g => g.Id == p.GroupId))
                .CountAsync();
            
            if (orphanedProfiles > 0)
            {
                // Clean up orphaned references
                var orphaned = await Profiles
                    .Where(p => p.GroupId.HasValue && !Groups.Any(g => g.Id == p.GroupId))
                    .ToListAsync();
                
                foreach (var profile in orphaned)
                {
                    profile.GroupId = null;
                }
                
                await SaveChangesAsync();
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}
