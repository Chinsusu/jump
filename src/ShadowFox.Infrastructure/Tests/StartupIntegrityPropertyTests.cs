using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Models;
using ShadowFox.Infrastructure.Configuration;
using ShadowFox.Infrastructure.Data;
using ShadowFox.Infrastructure.Services;
using Xunit;

namespace ShadowFox.Infrastructure.Tests;

public class StartupIntegrityPropertyTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IEncryptionService _encryptionService;

    public StartupIntegrityPropertyTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _encryptionService = new EncryptionService("test-key-for-integrity-tests");
        _context = new AppDbContext(options, _encryptionService);
    }

    /// <summary>
    /// **Feature: profile-management, Property 25: Startup integrity checks pass**
    /// **Validates: Requirements 8.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_StartupIntegrityChecksPass_EmptyDatabase()
    {
        try
        {
            // Test integrity check on a fresh/empty database
            var integrityCheckResult = _context.PerformIntegrityCheckAsync().GetAwaiter().GetResult();
            
            // Empty database should pass integrity checks
            return integrityCheckResult;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// **Feature: profile-management, Property 25: Startup integrity checks pass**
    /// **Validates: Requirements 8.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_StartupIntegrityChecksPass_WithValidData()
    {
        try
        {
            // Add some valid test data
            var group = new Group { Name = "TestGroup", Description = "Test Description" };
            _context.Groups.Add(group);
            _context.SaveChanges();

            var profile = Profile.CreateNew("TestProfile", "{}");
            profile.GroupId = group.Id;
            _context.Profiles.Add(profile);
            _context.SaveChanges();

            // Test integrity check with valid data
            var integrityCheckResult = _context.PerformIntegrityCheckAsync().GetAwaiter().GetResult();
            
            // Database with valid data should pass integrity checks
            return integrityCheckResult;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// **Feature: profile-management, Property 25: Startup integrity checks pass**
    /// **Validates: Requirements 8.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_StartupIntegrityChecksCleanupOrphanedReferences()
    {
        try
        {
            // Create a fresh context for this test to avoid interference
            using var testContext = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options, 
                _encryptionService);

            // Create a profile with a reference to a non-existent group
            var profile = Profile.CreateNew($"OrphanedProfile_{Guid.NewGuid()}", "{}");
            profile.GroupId = 999; // Non-existent group ID
            testContext.Profiles.Add(profile);
            testContext.SaveChanges();

            // Verify the orphaned reference exists before integrity check
            var profileBeforeCheck = testContext.Profiles.FirstOrDefault(p => p.GroupId == 999);
            if (profileBeforeCheck == null || profileBeforeCheck.GroupId != 999)
            {
                return false; // Setup failed
            }

            // Run integrity check - should clean up orphaned references
            var integrityCheckResult = testContext.PerformIntegrityCheckAsync().GetAwaiter().GetResult();
            
            if (!integrityCheckResult)
            {
                return false; // Integrity check failed
            }

            // Verify the orphaned reference was cleaned up
            var profileAfterCheck = testContext.Profiles.FirstOrDefault(p => p.Id == profileBeforeCheck.Id);
            
            // GroupId should be set to null after cleanup
            return profileAfterCheck != null && profileAfterCheck.GroupId == null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// **Feature: profile-management, Property 25: Startup integrity checks pass**
    /// **Validates: Requirements 8.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_StartupIntegrityChecksHandleDatabaseConnectivity()
    {
        try
        {
            // Test with a properly configured context
            var integrityCheckResult = _context.PerformIntegrityCheckAsync().GetAwaiter().GetResult();
            
            // Should be able to connect and perform checks
            return integrityCheckResult;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// **Feature: profile-management, Property 25: Startup integrity checks pass**
    /// **Validates: Requirements 8.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_StartupIntegrityChecksValidateDataCounts(PositiveInt profileCount, PositiveInt groupCount)
    {
        try
        {
            // Create a fresh context for this test to avoid interference
            using var testContext = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options, 
                _encryptionService);

            var numProfiles = Math.Min(profileCount.Get, 10); // Limit for test performance
            var numGroups = Math.Min(groupCount.Get, 5);

            // Add test groups
            var groups = new List<Group>();
            for (int i = 0; i < numGroups; i++)
            {
                var group = new Group { Name = $"TestGroup{i}_{Guid.NewGuid()}", Description = $"Description {i}" };
                testContext.Groups.Add(group);
                groups.Add(group);
            }
            testContext.SaveChanges();

            // Reload groups to get their IDs
            groups = testContext.Groups.ToList();

            // Add test profiles
            for (int i = 0; i < numProfiles; i++)
            {
                var profile = Profile.CreateNew($"TestProfile{i}_{Guid.NewGuid()}", "{}");
                if (groups.Count > 0 && i % 2 == 0) // Assign some profiles to groups
                {
                    profile.GroupId = groups[i % groups.Count].Id;
                }
                testContext.Profiles.Add(profile);
            }
            testContext.SaveChanges();

            // Run integrity check
            var integrityCheckResult = testContext.PerformIntegrityCheckAsync().GetAwaiter().GetResult();
            
            if (!integrityCheckResult)
            {
                return false;
            }

            // Verify counts are consistent
            var actualProfileCount = testContext.Profiles.Count();
            var actualGroupCount = testContext.Groups.Count();
            
            return actualProfileCount == numProfiles && actualGroupCount == numGroups;
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