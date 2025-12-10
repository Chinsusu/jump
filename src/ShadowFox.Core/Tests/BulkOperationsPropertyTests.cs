using FsCheck;
using FsCheck.Xunit;
using ShadowFox.Core.Models;
using ShadowFox.Core.Repositories;
using ShadowFox.Core.Services;
using System.Text.Json;
using Xunit;

namespace ShadowFox.Core.Tests;

public class BulkOperationsPropertyTests
{
    private readonly InMemoryProfileRepository _profileRepository;
    private readonly FingerprintGenerator _fingerprintGenerator;
    private readonly ProfileService _profileService;

    public BulkOperationsPropertyTests()
    {
        _profileRepository = new InMemoryProfileRepository();
        _fingerprintGenerator = new FingerprintGenerator();
        _profileService = new ProfileService(_profileRepository, _fingerprintGenerator);
    }

    /// <summary>
    /// **Feature: profile-management, Property 16: Bulk operations are atomic**
    /// **Validates: Requirements 5.3, 5.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property BulkOperationsAreAtomic()
    {
        return Prop.ForAll(
            Arb.From<PositiveInt>().Filter(x => x.Get >= 2 && x.Get <= 10), // Number of profiles to create (2-10)
            Arb.From<SpoofLevel>(),
            (profileCountGen, spoofLevel) =>
            {
                // Clear repository for each test
                _profileRepository.Clear();

                var profileCount = profileCountGen.Get;
                var createdProfiles = new List<Profile>();

                // Create test profiles
                for (int i = 0; i < profileCount; i++)
                {
                    var fingerprint = _fingerprintGenerator.Generate(spoofLevel);
                    var fingerprintJson = JsonSerializer.Serialize(fingerprint);
                    var profile = Profile.CreateNew($"TestProfile_{i}_{Guid.NewGuid()}", fingerprintJson);
                    var savedProfile = _profileRepository.AddAsync(profile).Result;
                    createdProfiles.Add(savedProfile);
                }

                // Test 1: Bulk delete with all valid IDs should succeed atomically
                var validIds = createdProfiles.Select(p => p.Id).ToArray();
                var deleteResult = _profileService.BulkDeleteAsync(validIds).Result;

                if (!deleteResult.IsSuccess)
                    return false.ToProperty();

                // All profiles should be deleted
                var remainingProfiles = _profileRepository.GetAllAsync().Result;
                var allDeleted = remainingProfiles.Count == 0;

                // The result should show all profiles were processed successfully
                var correctSuccessCount = deleteResult.Value!.SuccessCount == profileCount;
                var noFailures = deleteResult.Value!.FailedCount == 0;
                var correctProcessedIds = deleteResult.Value!.ProcessedIds.Count == profileCount;

                // Recreate profiles for next test
                createdProfiles.Clear();
                for (int i = 0; i < profileCount; i++)
                {
                    var fingerprint = _fingerprintGenerator.Generate(spoofLevel);
                    var fingerprintJson = JsonSerializer.Serialize(fingerprint);
                    var profile = Profile.CreateNew($"TestProfile2_{i}_{Guid.NewGuid()}", fingerprintJson);
                    var savedProfile = _profileRepository.AddAsync(profile).Result;
                    createdProfiles.Add(savedProfile);
                }

                // Test 2: Bulk delete with some invalid IDs should fail atomically (all or nothing)
                var mixedIds = createdProfiles.Take(profileCount / 2).Select(p => p.Id)
                    .Concat(new[] { 99999, 99998 }) // Non-existent IDs
                    .ToArray();

                var mixedDeleteResult = _profileService.BulkDeleteAsync(mixedIds).Result;

                // The operation should fail
                var operationFailed = !mixedDeleteResult.IsSuccess;

                // All original profiles should still exist (atomic rollback)
                var profilesAfterFailedDelete = _profileRepository.GetAllAsync().Result;
                var allProfilesStillExist = profilesAfterFailedDelete.Count == profileCount;

                // Test 3: Bulk tag update should be atomic
                var tagUpdateIds = createdProfiles.Select(p => p.Id).ToArray();
                var newTags = "test,atomic,operation";
                var tagUpdateResult = _profileService.BulkUpdateTagsAsync(tagUpdateIds, newTags).Result;

                if (!tagUpdateResult.IsSuccess)
                    return false.ToProperty();

                // All profiles should have the new tags
                var updatedProfiles = _profileRepository.GetAllAsync().Result;
                var allHaveNewTags = updatedProfiles.All(p => p.Tags == newTags);

                // Test 4: Bulk tag update with invalid IDs should fail atomically
                var mixedTagIds = createdProfiles.Take(profileCount / 2).Select(p => p.Id)
                    .Concat(new[] { 99999 }) // Non-existent ID
                    .ToArray();

                var mixedTagResult = _profileService.BulkUpdateTagsAsync(mixedTagIds, "should,not,apply").Result;

                // The operation should fail
                var tagOperationFailed = !mixedTagResult.IsSuccess;

                // Original tags should be preserved (atomic rollback)
                var profilesAfterFailedTagUpdate = _profileRepository.GetAllAsync().Result;
                var originalTagsPreserved = profilesAfterFailedTagUpdate.All(p => p.Tags == newTags);

                return allDeleted.ToProperty()
                    .And(correctSuccessCount.ToProperty())
                    .And(noFailures.ToProperty())
                    .And(correctProcessedIds.ToProperty())
                    .And(operationFailed.ToProperty())
                    .And(allProfilesStillExist.ToProperty())
                    .And(allHaveNewTags.ToProperty())
                    .And(tagOperationFailed.ToProperty())
                    .And(originalTagsPreserved.ToProperty());
            });
    }


}