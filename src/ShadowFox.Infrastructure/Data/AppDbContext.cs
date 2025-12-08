using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Models;

namespace ShadowFox.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Group> Groups => Set<Group>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profile>().Property(p => p.Tags).HasMaxLength(500);
        modelBuilder.Entity<Profile>().Property(p => p.Group).HasMaxLength(200);
        modelBuilder.Entity<Profile>().Property(p => p.Notes).HasMaxLength(1000);
        modelBuilder.Entity<Profile>().Property(p => p.FingerprintJson).HasMaxLength(4000);

        modelBuilder.Entity<Group>().HasIndex(g => g.Name).IsUnique();
        modelBuilder.Entity<Group>().Property(g => g.Name).HasMaxLength(200);
    }
}
