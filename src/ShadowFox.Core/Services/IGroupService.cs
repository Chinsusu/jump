using ShadowFox.Core.Common;
using ShadowFox.Core.Models;

namespace ShadowFox.Core.Services;

public interface IGroupService
{
    Task<Result<Group>> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<Result<Group[]>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<Group?>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result> ValidateGroupExistsAsync(int groupId, CancellationToken cancellationToken = default);
}