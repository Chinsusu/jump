using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Models;
using ShadowFox.Infrastructure.Data;

namespace ShadowFox.Infrastructure.Repositories;

public interface IProfileRepository
{
    Task<List<Profile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Profile?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Profile> AddAsync(Profile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(Profile profile, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ProfileRepository : IProfileRepository
{
    private readonly AppDbContext db;

    public ProfileRepository(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<List<Profile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Profiles
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Profile?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Profiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Profile> AddAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        db.Profiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task UpdateAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        db.Profiles.Update(profile);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Profiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity != null)
        {
            db.Profiles.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
