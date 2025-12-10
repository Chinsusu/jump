using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Common;
using ShadowFox.Core.Models;
using ShadowFox.Infrastructure.Data;
using ShadowFox.Infrastructure.Repositories;
using ShadowFox.Infrastructure.Services;
using Xunit;

namespace ShadowFox.Infrastructure.Tests;

public class GroupServiceIntegrationTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GroupService _groupService;
    private readonly GroupRepository _groupRepository;
    private readonly ProfileRepository _profileRepository;

    public GroupServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _groupRepository = new GroupRepository(_context);
        _profileRepository = new ProfileRepository(_context);
        _groupService = new GroupService(_groupRepository, _profileRepository);
    }

    [Fact]
    public async Task CreateGroup_WithValidName_ShouldSucceed()
    {
        // Arrange
        var groupName = "Test Group";

        // Act
        var result = await _groupService.CreateAsync(groupName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(groupName, result.Value.Name);
        Assert.True(result.Value.Id > 0);
    }

    [Fact]
    public async Task CreateGroup_WithDuplicateName_ShouldFail()
    {
        // Arrange
        var groupName = "Duplicate Group";
        await _groupService.CreateAsync(groupName);

        // Act
        var result = await _groupService.CreateAsync(groupName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCode.DuplicateEntity, result.ErrorCode);
    }

    [Fact]
    public async Task DeleteGroup_WithAssociatedProfiles_ShouldRemoveGroupReferences()
    {
        // Arrange
        var groupResult = await _groupService.CreateAsync("Test Group");
        var group = groupResult.Value!;

        var profile1 = Profile.CreateNew("Profile 1", "{\"test\":\"data\"}");
        profile1.GroupId = group.Id;
        await _profileRepository.AddAsync(profile1);

        var profile2 = Profile.CreateNew("Profile 2", "{\"test\":\"data\"}");
        profile2.GroupId = group.Id;
        await _profileRepository.AddAsync(profile2);

        // Act
        var deleteResult = await _groupService.DeleteAsync(group.Id);

        // Assert
        Assert.True(deleteResult.IsSuccess);

        // Verify profiles no longer reference the group
        var updatedProfile1 = await _profileRepository.GetByIdAsync(profile1.Id);
        var updatedProfile2 = await _profileRepository.GetByIdAsync(profile2.Id);
        
        Assert.Null(updatedProfile1!.GroupId);
        Assert.Null(updatedProfile2!.GroupId);

        // Verify group is deleted
        var deletedGroup = await _groupRepository.GetByIdAsync(group.Id);
        Assert.Null(deletedGroup);
    }

    [Fact]
    public async Task ValidateGroupExists_WithExistingGroup_ShouldReturnSuccess()
    {
        // Arrange
        var groupResult = await _groupService.CreateAsync("Existing Group");
        var group = groupResult.Value!;

        // Act
        var validationResult = await _groupService.ValidateGroupExistsAsync(group.Id);

        // Assert
        Assert.True(validationResult.IsSuccess);
    }

    [Fact]
    public async Task ValidateGroupExists_WithNonExistentGroup_ShouldReturnFailure()
    {
        // Act
        var validationResult = await _groupService.ValidateGroupExistsAsync(999);

        // Assert
        Assert.False(validationResult.IsSuccess);
        Assert.Equal(ErrorCode.NotFound, validationResult.ErrorCode);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}