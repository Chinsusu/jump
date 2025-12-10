using ShadowFox.Core.Models;

namespace ShadowFox.Core.Repositories;

public interface IProfileRepository
{
    Task<List<Profile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Profile>> GetAllAsync(ProfileFilter filter, CancellationToken cancellationToken = default);
    Task<Profile?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Profile> AddAsync(Profile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(Profile profile, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Profile>> GetByGroupIdAsync(int groupId, CancellationToken cancellationToken = default);
    Task BulkUpdateAsync(int[] ids, Action<Profile> updateAction, CancellationToken cancellationToken = default);
}