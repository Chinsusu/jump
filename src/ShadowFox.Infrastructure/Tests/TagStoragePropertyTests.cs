using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Models;
using ShadowFox.Infrastructure.Data;
using ShadowFox.Infrastructure.Repositories;
using ShadowFox.Infrastructure.Services;
using Xunit;

namespace ShadowFox.Infrastructure.Tests;

public class TagStoragePropertyTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ProfileRepository _repository;

    public TagStoragePropertyTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var encryptionService = new EncryptionService("test-key-for-property-tests");
        _context = new AppDbContext(options, encryptionService);
        _repository = new ProfileRepository(_context);
    }

    /// <summary>
    /// **Feature: profile-management, Property 9: Tags are stored as comma-separated strings**
    /// **Validates: Requirements 3.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_TagsAreStoredAsCommaSeparatedStrings(NonEmptyString profileName, NonEmptyString fingerprintJson, string[] tags)
    {
        // Arrange - Filter out null/empty tags and remove commas to ensure valid tags
        var validTags = tags?.Where(t => !string.IsNullOrWhiteSpace(t))
                            .Select(t => t.Trim().Replace(",", "")) // Remove commas to avoid conflicts
                            .Where(t => t.Length > 0)
                            .ToArray() ?? Array.Empty<string>();
        
        if (validTags.Length == 0)
        {
            // If no valid tags, test with null tags
            return TestTagStorage(profileName.Get, fingerprintJson.Get, null, null);
        }

        // Create comma-separated string using the SetTags extension method format
        var expectedTagString = string.Join(", ", validTags); // Note: space after comma as per SetTags method
        
        return TestTagStorage(profileName.Get, fingerprintJson.Get, expectedTagString, validTags);
    }

    private bool TestTagStorage(string profileName, string fingerprintJson, string? tagString, string[]? expectedTags)
    {
        try
        {
            // Ensure name is unique by appending a GUID
            var uniqueName = $"{profileName}_{Guid.NewGuid()}";
            
            var profile = Profile.CreateNew(uniqueName, fingerprintJson, tags: tagString);

            // Act - Add profile to repository
            var addedProfile = _repository.AddAsync(profile).GetAwaiter().GetResult();

            // Assert - Profile should be stored with tags as comma-separated string
            var retrievedProfile = _repository.GetByIdAsync(addedProfile.Id).GetAwaiter().GetResult();
            
            if (retrievedProfile == null)
                return false;

            // Check that tags are stored as expected
            if (tagString == null)
            {
                return retrievedProfile.Tags == null;
            }

            if (retrievedProfile.Tags != tagString)
                return false;

            // Verify that the profile can parse tags correctly using extension method
            if (expectedTags != null)
            {
                var parsedTags = retrievedProfile.GetTagsArray();
                return parsedTags.SequenceEqual(expectedTags);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}