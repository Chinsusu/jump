using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Models;
using ShadowFox.Core.Repositories;
using ShadowFox.Core.Services;
using ShadowFox.Infrastructure.Data;
using ShadowFox.Infrastructure.Repositories;
using ShadowFox.Infrastructure.Services;
using Xunit;

namespace ShadowFox.Infrastructure.Tests;

public class GroupPropertyTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GroupService _groupService;
    private readonly IGroupRepository _groupRepository;
    private readonly IProfileRepository _profileRepository;

    public GroupPropertyTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _groupRepository = new GroupRepository(_context);
        _profileRepository = new ProfileRepository(_context);
        _groupService = new GroupService(_groupRepository, _profileRepository);
    }

    /// <summary>
    /// **Feature: profile-management, Property 8: Group assignment validates existence**
    /// **Validates: Requirements 3.1, 5.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property GroupAssignmentValidatesExistence()
    {
        return Prop.ForAll(
            Arb.From<int>().Filter(id => id > 0),
            (groupId) =>
            {
                // Clear database for each test
                _context.Groups.RemoveRange(_context.Groups);
                _context.Profiles.RemoveRange(_context.Profiles);
                _context.SaveChanges();

                // Test validation of non-existent group
                var validationResult = _groupService.ValidateGroupExistsAsync(groupId).Result;
                var nonExistentGroupRejected = !validationResult.IsSuccess;

                // Create a group
                var group = new Group { Name = $"TestGroup_{groupId}", CreatedAt = DateTime.UtcNow };
                _groupRepository.AddAsync(group).Wait();

                // Test validation of existing group
                var validationResult2 = _groupService.ValidateGroupExistsAsync(group.Id).Result;
                var existingGroupAccepted = validationResult2.IsSuccess;

                // Create a profile and try to assign non-existent group
                var profile = Profile.CreateNew($"TestProfile_{groupId}", "{\"test\":\"data\"}");
                profile.GroupId = groupId + 1000; // Use a definitely non-existent ID
                _profileRepository.AddAsync(profile).Wait();

                // Validate that we can detect the invalid group assignment
                var profileValidation = _groupService.ValidateGroupExistsAsync(profile.GroupId.Value).Result;
                var invalidAssignmentDetected = !profileValidation.IsSuccess;

                return nonExistentGroupRejected.ToProperty()
                    .And(existingGroupAccepted.ToProperty())
                    .And(invalidAssignmentDetected.ToProperty());
            });
    }

    /// <summary>
    /// **Feature: profile-management, Property 11: Group deletion updates profile references**
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property GroupDeletionUpdatesProfileReferences()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 200),
            Arb.From<int>().Filter(count => count >= 1 && count <= 10),
            (groupName, profileCount) =>
            {
                // Clear database for each test
                _context.Groups.RemoveRange(_context.Groups);
                _context.Profiles.RemoveRange(_context.Profiles);
                _context.SaveChanges();

                var trimmedGroupName = groupName.Trim();

                // Create a group
                var createResult = _groupService.CreateAsync(trimmedGroupName).Result;
                if (!createResult.IsSuccess) return false.ToProperty();

                var group = createResult.Value!;

                // Create multiple profiles assigned to this group
                var profiles = new List<Profile>();
                for (int i = 0; i < profileCount; i++)
                {
                    var profile = Profile.CreateNew($"Profile_{i}_{trimmedGroupName}", "{\"test\":\"data\"}");
                    profile.GroupId = group.Id;
                    _profileRepository.AddAsync(profile).Wait();
                    profiles.Add(profile);
                }

                // Verify profiles are assigned to the group
                var profilesBeforeDeletion = _profileRepository.GetByGroupIdAsync(group.Id).Result;
                var allProfilesAssigned = profilesBeforeDeletion.Count == profileCount &&
                                        profilesBeforeDeletion.All(p => p.GroupId == group.Id);

                // Delete the group
                var deleteResult = _groupService.DeleteAsync(group.Id).Result;
                var deletionSuccessful = deleteResult.IsSuccess;

                // Verify all profiles no longer reference the deleted group
                var allProfiles = _profileRepository.GetAllAsync().Result;
                var profilesWithNullGroup = allProfiles.Where(p => profiles.Any(orig => orig.Name == p.Name))
                                                     .All(p => p.GroupId == null);

                // Verify the group no longer exists
                var groupAfterDeletion = _groupRepository.GetByIdAsync(group.Id).Result;
                var groupDeleted = groupAfterDeletion == null;

                return allProfilesAssigned.ToProperty()
                    .And(deletionSuccessful.ToProperty())
                    .And(profilesWithNullGroup.ToProperty())
                    .And(groupDeleted.ToProperty());
            });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}