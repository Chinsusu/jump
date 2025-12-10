using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Models;
using ShadowFox.Core.Repositories;
using ShadowFox.Core.Services;
using ShadowFox.Infrastructure.Data;
using ShadowFox.Infrastructure.Repositories;
using System.Text.Json;
using Xunit;

namespace ShadowFox.Infrastructure.Tests;

public class ProfileServicePropertyTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IProfileRepository _profileRepository;
    private readonly FingerprintGenerator _fingerprintGenerator;
    private readonly ProfileService _profileService;

    public ProfileServicePropertyTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _profileRepository = new ProfileRepository(_context);
        _fingerprintGenerator = new FingerprintGenerator();
        _profileService = new ProfileService(_profileRepository, _fingerprintGenerator);
    }

    /// <summary>
    /// **Feature: profile-management, Property 6: Clone names are automatically modified**
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CloneNamesAreAutomaticallyModified()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 190), // Leave room for suffix
            Arb.From<SpoofLevel>(),
            (baseName, spoofLevel) =>
            {
                // Clear database for each test
                _context.Profiles.RemoveRange(_context.Profiles);
                _context.SaveChanges();

                var trimmedName = baseName.Trim();
                
                // Create a source profile in the database
                var fingerprint = _fingerprintGenerator.Generate(spoofLevel);
                var fingerprintJson = JsonSerializer.Serialize(fingerprint);
                var sourceProfile = Profile.CreateNew(trimmedName, fingerprintJson);
                _profileRepository.AddAsync(sourceProfile).Wait();

                // Test cloning with the same name as source
                var cloneResult = _profileService.CloneAsync(sourceProfile.Id, trimmedName).Result;

                if (!cloneResult.IsSuccess)
                    return false.ToProperty();

                var clonedProfile = cloneResult.Value!;

                // The cloned profile name should be different from the source name
                var nameIsModified = clonedProfile.Name != sourceProfile.Name;
                
                // The cloned profile name should contain the original name
                var nameContainsOriginal = clonedProfile.Name.Contains(trimmedName);
                
                // The cloned profile should have a suffix indicating it's a copy
                var nameHasCopySuffix = clonedProfile.Name.Contains("(") && clonedProfile.Name.Contains(")");

                return nameIsModified.ToProperty()
                    .And(nameContainsOriginal.ToProperty())
                    .And(nameHasCopySuffix.ToProperty());
            });
    }

    /// <summary>
    /// **Feature: profile-management, Property 7: Clone timestamps are updated**
    /// **Validates: Requirements 2.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CloneTimestampsAreUpdated()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 190),
            Arb.From<SpoofLevel>(),
            (baseName, spoofLevel) =>
            {
                // Clear database for each test
                _context.Profiles.RemoveRange(_context.Profiles);
                _context.SaveChanges();

                var trimmedName = baseName.Trim();
                
                // Create a source profile with an older timestamp
                var fingerprint = _fingerprintGenerator.Generate(spoofLevel);
                var fingerprintJson = JsonSerializer.Serialize(fingerprint);
                var sourceProfile = Profile.CreateNew(trimmedName, fingerprintJson);
                
                // Set source profile to have an older timestamp
                var oldTimestamp = DateTime.UtcNow.AddHours(-1);
                sourceProfile.CreatedAt = oldTimestamp;
                sourceProfile.LastModifiedAt = oldTimestamp;

                _profileRepository.AddAsync(sourceProfile).Wait();

                var cloneTimestamp = DateTime.UtcNow;
                
                // Test cloning
                var cloneResult = _profileService.CloneAsync(sourceProfile.Id, $"{trimmedName} Clone").Result;

                if (!cloneResult.IsSuccess)
                    return false.ToProperty();

                var clonedProfile = cloneResult.Value!;

                // The cloned profile should have a newer creation timestamp
                var creationTimestampIsNewer = clonedProfile.CreatedAt > sourceProfile.CreatedAt;
                
                // The cloned profile creation timestamp should be close to current time (within 1 minute)
                var creationTimestampIsRecent = Math.Abs((clonedProfile.CreatedAt - cloneTimestamp).TotalMinutes) < 1;
                
                // The cloned profile should have a newer last modified timestamp
                var modifiedTimestampIsNewer = clonedProfile.LastModifiedAt > sourceProfile.LastModifiedAt;
                
                // The cloned profile modified timestamp should be close to current time (within 1 minute)
                var modifiedTimestampIsRecent = Math.Abs((clonedProfile.LastModifiedAt - cloneTimestamp).TotalMinutes) < 1;

                return creationTimestampIsNewer.ToProperty()
                    .And(creationTimestampIsRecent.ToProperty())
                    .And(modifiedTimestampIsNewer.ToProperty())
                    .And(modifiedTimestampIsRecent.ToProperty());
            });
    }

    /// <summary>
    /// **Feature: profile-management, Property 18: Modifications update timestamps**
    /// **Validates: Requirements 6.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ModificationsUpdateTimestamps(PositiveInt originalNameLength, PositiveInt newNameLength, SpoofLevel spoofLevel)
    {
        // Clear database for each test
        _context.Profiles.RemoveRange(_context.Profiles);
        _context.SaveChanges();

        // Generate valid names with different characters to ensure they're different
        var originalName = "Profile_" + new string('A', Math.Min(originalNameLength.Get % 50 + 1, 190));
        var newName = "Profile_" + new string('B', Math.Min(newNameLength.Get % 50 + 1, 190));
        
        // Skip if names are the same (no modification)
        if (originalName == newName)
            return true;
        
        // Create an existing profile with an older timestamp
        var fingerprint = _fingerprintGenerator.Generate(spoofLevel);
        var fingerprintJson = JsonSerializer.Serialize(fingerprint);
        var existingProfile = Profile.CreateNew(originalName, fingerprintJson);
        
        // Set profile to have an older timestamp
        var oldTimestamp = DateTime.UtcNow.AddHours(-1);
        existingProfile.CreatedAt = oldTimestamp;
        existingProfile.LastModifiedAt = oldTimestamp;

        _profileRepository.AddAsync(existingProfile).Wait();

        var updateTimestamp = DateTime.UtcNow;
        
        // Test updating the profile name
        var updateRequest = new UpdateProfileRequest { Name = newName };
        var updateResult = _profileService.UpdateAsync(existingProfile.Id, updateRequest).Result;

        if (!updateResult.IsSuccess)
            return false;

        var updatedProfile = updateResult.Value!;

        // The profile name should be updated
        var nameWasUpdated = updatedProfile.Name == newName;
        
        // The creation timestamp should remain unchanged
        var creationTimestampUnchanged = updatedProfile.CreatedAt == oldTimestamp;
        
        // The last modified timestamp should be updated to current time
        var modifiedTimestampIsNewer = updatedProfile.LastModifiedAt > oldTimestamp;
        
        // The modified timestamp should be close to current time (within 1 minute)
        var modifiedTimestampIsRecent = Math.Abs((updatedProfile.LastModifiedAt - updateTimestamp).TotalMinutes) < 1;

        return nameWasUpdated && creationTimestampUnchanged && modifiedTimestampIsNewer && modifiedTimestampIsRecent;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}