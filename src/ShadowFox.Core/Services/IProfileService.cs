using ShadowFox.Core.Common;
using ShadowFox.Core.Models;

namespace ShadowFox.Core.Services;

public interface IProfileService
{
    Task<Result<Profile>> CreateAsync(CreateProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result<Profile>> CloneAsync(int sourceId, string newName, CancellationToken cancellationToken = default);
    Task<Result<Profile>> UpdateAsync(int id, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<Profile?>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<List<Profile>>> GetAllAsync(ProfileFilter? filter = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> ExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<Result<string>> ExportAsync(int[] profileIds, CancellationToken cancellationToken = default);
    Task<Result<ImportResult>> ImportAsync(string jsonData, CancellationToken cancellationToken = default);
    Task<Result<BulkOperationResult>> BulkDeleteAsync(int[] profileIds, CancellationToken cancellationToken = default);
    Task<Result<BulkOperationResult>> BulkUpdateTagsAsync(int[] profileIds, string tags, CancellationToken cancellationToken = default);
    Task<Result<BulkOperationResult>> BulkAssignGroupAsync(int[] profileIds, int? groupId, CancellationToken cancellationToken = default);
    Task<Result> RecordProfileAccessAsync(int profileId, CancellationToken cancellationToken = default);
}

public class CreateProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public SpoofLevel SpoofLevel { get; set; } = SpoofLevel.Ultra;
    public int? GroupId { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
}

public class UpdateProfileRequest
{
    public string? Name { get; set; }
    public string? FingerprintJson { get; set; }
    public int? GroupId { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

public class ImportResult
{
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<Profile> ImportedProfiles { get; set; } = new();
}

public class BulkOperationResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<int> ProcessedIds { get; set; } = new();
}