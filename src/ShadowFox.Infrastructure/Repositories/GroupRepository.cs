using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Models;
using ShadowFox.Core.Repositories;
using ShadowFox.Infrastructure.Data;

namespace ShadowFox.Infrastructure.Repositories;

public sealed class GroupRepository : IGroupRepository
{
    private readonly AppDbContext db;

    public GroupRepository(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<List<Group>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Groups
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Group?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Groups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default) =>
        db.Groups.AnyAsync(g => g.Name == name, cancellationToken);

    public async Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        db.Groups.Add(group);
        await db.SaveChangesAsync(cancellationToken);
        return group;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Groups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (entity != null)
        {
            db.Groups.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
