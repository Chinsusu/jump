using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Models;
using ShadowFox.Infrastructure.Data;
using ShadowFox.Infrastructure.Repositories;
using ShadowFox.Infrastructure.Services;
using Xunit;

namespace ShadowFox.Infrastructure.Tests;

public class ProfilePersistencePropertyTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ProfileRepository _repository;

    public ProfilePersistencePropertyTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var encryptionService = new EncryptionService("test-key-for-property-tests");
        _context = new AppDbContext(options, encryptionService);
        _repository = new ProfileRepository(_context);
    }

    /// <summary>
    /// **Feature: profile-management, Property 4: Profile persistence is immediate**
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ProfilePersistenceIsImmediate(NonEmptyString profileName, NonEmptyString fingerprintJson)
    {
        // Arrange
        var name = profileName.Get;
        var fingerprint = fingerprintJson.Get;
        
        // Ensure name is unique by appending a GUID
        var uniqueName = $"{name}_{Guid.NewGuid()}";
        
        var profile = Profile.CreateNew(uniqueName, fingerprint);

        // Act - Add profile to repository
        var addedProfile = _repository.AddAsync(profile).GetAwaiter().GetResult();

        // Assert - Profile should be immediately queryable from database
        var retrievedProfile = _repository.GetByIdAsync(addedProfile.Id).GetAwaiter().GetResult();
        
        return retrievedProfile != null &&
               retrievedProfile.Name == uniqueName &&
               retrievedProfile.FingerprintJson == fingerprint &&
               retrievedProfile.Id > 0 &&
               retrievedProfile.CreatedAt <= DateTime.UtcNow &&
               retrievedProfile.LastModifiedAt <= DateTime.UtcNow;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}