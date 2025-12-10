using ShadowFox.Core.Models;

namespace ShadowFox.Core.Repositories;

public interface IGroupRepository
{
    Task<List<Group>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Group?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}