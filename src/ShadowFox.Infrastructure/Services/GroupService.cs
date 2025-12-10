using ShadowFox.Core.Common;
using ShadowFox.Core.Models;
using ShadowFox.Core.Repositories;
using ShadowFox.Core.Services;

namespace ShadowFox.Infrastructure.Services;

public sealed class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IProfileRepository _profileRepository;

    public GroupService(IGroupRepository groupRepository, IProfileRepository profileRepository)
    {
        _groupRepository = groupRepository;
        _profileRepository = profileRepository;
    }

    public async Task<Result<Group>> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Group>.Failure("Group name cannot be empty", ErrorCode.RequiredFieldMissing);
        }

        if (name.Length > 200)
        {
            return Result<Group>.Failure("Group name cannot exceed 200 characters", ErrorCode.ValueOutOfRange);
        }

        // Check for duplicate name
        var exists = await _groupRepository.ExistsAsync(name, cancellationToken);
        if (exists)
        {
            return Result<Group>.Failure($"A group with the name '{name}' already exists", ErrorCode.DuplicateEntity);
        }

        try
        {
            var group = new Group
            {
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            var createdGroup = await _groupRepository.AddAsync(group, cancellationToken);
            return Result<Group>.Success(createdGroup);
        }
        catch (Exception ex)
        {
            return Result<Group>.Failure($"Failed to create group: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<Group[]>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = await _groupRepository.GetAllAsync(cancellationToken);
            return Result<Group[]>.Success(groups.ToArray());
        }
        catch (Exception ex)
        {
            return Result<Group[]>.Failure($"Failed to retrieve groups: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            // First, update all profiles that reference this group to remove the group reference
            var profiles = await _profileRepository.GetAllAsync(cancellationToken);
            var profilesToUpdate = profiles.Where(p => p.GroupId == id).ToList();

            foreach (var profile in profilesToUpdate)
            {
                profile.GroupId = null;
                profile.UpdateModified();
                await _profileRepository.UpdateAsync(profile, cancellationToken);
            }

            // Then delete the group
            await _groupRepository.DeleteAsync(id, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete group: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<Group?>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var group = await _groupRepository.GetByIdAsync(id, cancellationToken);
            return Result<Group?>.Success(group);
        }
        catch (Exception ex)
        {
            return Result<Group?>.Failure($"Failed to retrieve group: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result> ValidateGroupExistsAsync(int groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var groupResult = await GetByIdAsync(groupId, cancellationToken);
            if (!groupResult.IsSuccess)
            {
                return Result.Failure(groupResult.ErrorMessage!, groupResult.ErrorCode);
            }

            if (groupResult.Value == null)
            {
                return Result.Failure($"Group with ID {groupId} does not exist", ErrorCode.NotFound);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to validate group existence: {ex.Message}", ErrorCode.DatabaseError);
        }
    }
}