using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShadowFox.Core.Models;
using ShadowFox.Core.Repositories;
using ShadowFox.Infrastructure.Data;

namespace ShadowFox.Infrastructure.Repositories;

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
            .Include(p => p.Group)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Profile>> GetAllAsync(ProfileFilter filter, CancellationToken cancellationToken = default)
    {
        var query = db.Profiles.Include(p => p.Group).AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var searchTerm = filter.SearchQuery.ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(searchTerm) ||
                (p.Tags != null && p.Tags.ToLower().Contains(searchTerm)) ||
                (p.Group != null && p.Group.Name.ToLower().Contains(searchTerm)));
        }

        if (filter.GroupId.HasValue)
        {
            query = query.Where(p => p.GroupId == filter.GroupId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.GroupName))
        {
            query = query.Where(p => p.Group != null && p.Group.Name == filter.GroupName);
        }

        if (filter.Tags?.Length > 0)
        {
            foreach (var tag in filter.Tags)
            {
                query = query.Where(p => p.Tags != null && p.Tags.Contains(tag));
            }
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == filter.IsActive.Value);
        }

        if (filter.CreatedAfter.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= filter.CreatedAfter.Value);
        }

        if (filter.CreatedBefore.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= filter.CreatedBefore.Value);
        }

        if (filter.LastOpenedAfter.HasValue)
        {
            query = query.Where(p => p.LastOpenedAt >= filter.LastOpenedAfter.Value);
        }

        if (filter.LastOpenedBefore.HasValue)
        {
            query = query.Where(p => p.LastOpenedAt <= filter.LastOpenedBefore.Value);
        }

        if (filter.NeverUsed.HasValue && filter.NeverUsed.Value)
        {
            query = query.Where(p => p.LastOpenedAt == null);
        }

        // Apply sorting
        query = filter.SortBy switch
        {
            ProfileSortBy.Name => filter.SortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.Name) 
                : query.OrderByDescending(p => p.Name),
            ProfileSortBy.CreatedAt => filter.SortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.CreatedAt) 
                : query.OrderByDescending(p => p.CreatedAt),
            ProfileSortBy.LastOpenedAt => filter.SortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.LastOpenedAt) 
                : query.OrderByDescending(p => p.LastOpenedAt),
            ProfileSortBy.LastModifiedAt => filter.SortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.LastModifiedAt) 
                : query.OrderByDescending(p => p.LastModifiedAt),
            ProfileSortBy.UsageCount => filter.SortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.UsageCount) 
                : query.OrderByDescending(p => p.UsageCount),
            ProfileSortBy.GroupName => filter.SortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.Group != null ? p.Group.Name : "") 
                : query.OrderByDescending(p => p.Group != null ? p.Group.Name : ""),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        // Apply pagination
        if (filter.Skip.HasValue)
        {
            query = query.Skip(filter.Skip.Value);
        }

        if (filter.Take.HasValue)
        {
            query = query.Take(filter.Take.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<Profile?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Profiles.Include(p => p.Group).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Profile?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        db.Profiles.Include(p => p.Group).FirstOrDefaultAsync(p => p.Name == name, cancellationToken);

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

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default) =>
        db.Profiles.AnyAsync(p => p.Name == name, cancellationToken);

    public async Task<List<Profile>> GetByGroupIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return await db.Profiles
            .Include(p => p.Group)
            .Where(p => p.GroupId == groupId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task BulkUpdateAsync(int[] ids, Action<Profile> updateAction, CancellationToken cancellationToken = default)
    {
        var profiles = await db.Profiles.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
        
        foreach (var profile in profiles)
        {
            updateAction(profile);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> GroupExistsAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return await db.Groups.AnyAsync(g => g.Id == groupId, cancellationToken);
    }

    public async Task<(bool Success, int ProcessedCount, List<string> Errors)> BulkDeleteAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var processedCount = 0;

        try
        {
            // Try to use transaction if supported, otherwise proceed without it
            IDbContextTransaction? transaction = null;
            try
            {
                transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // In-memory database doesn't support transactions, continue without it
            }

            try
            {
                // Verify all profiles exist before deleting any
                var existingProfiles = await db.Profiles.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
                
                if (existingProfiles.Count != ids.Length)
                {
                    var foundIds = existingProfiles.Select(p => p.Id).ToHashSet();
                    var missingIds = ids.Where(id => !foundIds.Contains(id));
                    foreach (var missingId in missingIds)
                    {
                        errors.Add($"Profile with ID {missingId} not found.");
                    }
                    if (transaction != null)
                        await transaction.RollbackAsync(cancellationToken);
                    return (false, 0, errors);
                }

                // Delete all profiles
                db.Profiles.RemoveRange(existingProfiles);
                await db.SaveChangesAsync(cancellationToken);
                processedCount = existingProfiles.Count;

                if (transaction != null)
                    await transaction.CommitAsync(cancellationToken);
                return (true, processedCount, errors);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await transaction.RollbackAsync(cancellationToken);
                errors.Add($"Database error: {ex.Message}");
                return (false, 0, errors);
            }
            finally
            {
                transaction?.Dispose();
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Database error: {ex.Message}");
            return (false, 0, errors);
        }
    }

    public async Task<(bool Success, int ProcessedCount, List<string> Errors)> BulkUpdateTagsAsync(int[] ids, string tags, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var processedCount = 0;

        try
        {
            // Try to use transaction if supported, otherwise proceed without it
            IDbContextTransaction? transaction = null;
            try
            {
                transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // In-memory database doesn't support transactions, continue without it
            }

            try
            {
                // Verify all profiles exist before updating any
                var existingProfiles = await db.Profiles.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
                
                if (existingProfiles.Count != ids.Length)
                {
                    var foundIds = existingProfiles.Select(p => p.Id).ToHashSet();
                    var missingIds = ids.Where(id => !foundIds.Contains(id));
                    foreach (var missingId in missingIds)
                    {
                        errors.Add($"Profile with ID {missingId} not found.");
                    }
                    if (transaction != null)
                        await transaction.RollbackAsync(cancellationToken);
                    return (false, 0, errors);
                }

                // Update tags for all profiles
                foreach (var profile in existingProfiles)
                {
                    profile.Tags = string.IsNullOrWhiteSpace(tags) ? null : tags;
                    profile.UpdateModified();
                }

                await db.SaveChangesAsync(cancellationToken);
                processedCount = existingProfiles.Count;

                if (transaction != null)
                    await transaction.CommitAsync(cancellationToken);
                return (true, processedCount, errors);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await transaction.RollbackAsync(cancellationToken);
                errors.Add($"Database error: {ex.Message}");
                return (false, 0, errors);
            }
            finally
            {
                transaction?.Dispose();
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Database error: {ex.Message}");
            return (false, 0, errors);
        }
    }

    public async Task<(bool Success, int ProcessedCount, List<string> Errors)> BulkAssignGroupAsync(int[] ids, int? groupId, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var processedCount = 0;

        try
        {
            // Try to use transaction if supported, otherwise proceed without it
            IDbContextTransaction? transaction = null;
            try
            {
                transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // In-memory database doesn't support transactions, continue without it
            }

            try
            {
                // Verify all profiles exist before updating any
                var existingProfiles = await db.Profiles.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
                
                if (existingProfiles.Count != ids.Length)
                {
                    var foundIds = existingProfiles.Select(p => p.Id).ToHashSet();
                    var missingIds = ids.Where(id => !foundIds.Contains(id));
                    foreach (var missingId in missingIds)
                    {
                        errors.Add($"Profile with ID {missingId} not found.");
                    }
                    if (transaction != null)
                        await transaction.RollbackAsync(cancellationToken);
                    return (false, 0, errors);
                }

                // If groupId is provided, validate that the group exists
                if (groupId.HasValue && groupId.Value > 0)
                {
                    var groupExists = await db.Groups.AnyAsync(g => g.Id == groupId.Value, cancellationToken);
                    if (!groupExists)
                    {
                        errors.Add($"Group with ID {groupId.Value} not found.");
                        if (transaction != null)
                            await transaction.RollbackAsync(cancellationToken);
                        return (false, 0, errors);
                    }
                }

                // Update group assignment for all profiles
                foreach (var profile in existingProfiles)
                {
                    profile.GroupId = groupId == 0 ? null : groupId;
                    profile.UpdateModified();
                }

                await db.SaveChangesAsync(cancellationToken);
                processedCount = existingProfiles.Count;

                if (transaction != null)
                    await transaction.CommitAsync(cancellationToken);
                return (true, processedCount, errors);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await transaction.RollbackAsync(cancellationToken);
                errors.Add($"Database error: {ex.Message}");
                return (false, 0, errors);
            }
            finally
            {
                transaction?.Dispose();
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Database error: {ex.Message}");
            return (false, 0, errors);
        }
    }
}
