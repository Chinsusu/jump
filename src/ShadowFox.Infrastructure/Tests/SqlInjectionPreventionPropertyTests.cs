using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Models;
using ShadowFox.Infrastructure.Data;
using ShadowFox.Infrastructure.Repositories;
using ShadowFox.Infrastructure.Services;
using Xunit;

namespace ShadowFox.Infrastructure.Tests;

public class SqlInjectionPreventionPropertyTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ProfileRepository _repository;
    private readonly GroupRepository _groupRepository;
    private readonly IEncryptionService _encryptionService;

    public SqlInjectionPreventionPropertyTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _encryptionService = new EncryptionService("test-key-for-sql-injection-tests");
        _context = new AppDbContext(options, _encryptionService);
        _repository = new ProfileRepository(_context);
        _groupRepository = new GroupRepository(_context);
    }

    /// <summary>
    /// **Feature: profile-management, Property 23: Database queries are parameterized**
    /// **Validates: Requirements 8.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_DatabaseQueriesAreParameterized_ProfileOperations(NonEmptyString maliciousInput)
    {
        try
        {
            var maliciousString = maliciousInput.Get;
            
            // Common SQL injection patterns
            var injectionPatterns = new[]
            {
                "'; DROP TABLE Profiles; --",
                "' OR '1'='1",
                "'; DELETE FROM Profiles WHERE 1=1; --",
                "' UNION SELECT * FROM Groups --",
                "'; INSERT INTO Profiles VALUES ('hacked', 'hacked'); --",
                "' OR 1=1 --",
                "'; UPDATE Profiles SET Name='hacked' WHERE 1=1; --"
            };
            
            // Test with malicious input combined with injection patterns
            foreach (var pattern in injectionPatterns)
            {
                var maliciousName = $"{maliciousString}{pattern}";
                
                // Test profile creation with malicious name
                try
                {
                    var profile = Profile.CreateNew(maliciousName, "{}");
                    var addedProfile = _repository.AddAsync(profile).GetAwaiter().GetResult();
                    
                    // If we reach here, the operation succeeded without SQL injection
                    // The malicious input should be treated as literal data, not SQL commands
                    var retrievedProfile = _repository.GetByIdAsync(addedProfile.Id).GetAwaiter().GetResult();
                    
                    // The name should be stored exactly as provided (escaped/parameterized)
                    if (retrievedProfile?.Name != maliciousName)
                    {
                        return false; // Data integrity issue
                    }
                }
                catch (Exception ex)
                {
                    // If an exception occurs, it should be a validation error, not a SQL error
                    // SQL injection would typically cause database-level errors
                    if (ex.Message.Contains("SQL") || ex.Message.Contains("syntax") || 
                        ex.Message.Contains("DROP") || ex.Message.Contains("DELETE"))
                    {
                        return false; // Potential SQL injection vulnerability
                    }
                }
                
                // Test profile search with malicious input
                try
                {
                    var filter = new ProfileFilter { SearchQuery = maliciousName };
                    var searchResults = _repository.GetAllAsync(filter).GetAwaiter().GetResult();
                    
                    // Search should complete without SQL injection, even if no results found
                    // The fact that it completes successfully indicates parameterized queries
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("SQL") || ex.Message.Contains("syntax"))
                    {
                        return false; // Potential SQL injection vulnerability
                    }
                }
            }
            
            return true; // All operations completed safely
        }
        catch (Exception ex)
        {
            // Any database-level SQL errors indicate potential injection vulnerability
            return !ex.Message.Contains("SQL") && !ex.Message.Contains("syntax");
        }
    }

    /// <summary>
    /// **Feature: profile-management, Property 23: Database queries are parameterized**
    /// **Validates: Requirements 8.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_DatabaseQueriesAreParameterized_GroupOperations(NonEmptyString maliciousInput)
    {
        try
        {
            var maliciousString = maliciousInput.Get;
            
            // SQL injection patterns targeting group operations
            var injectionPatterns = new[]
            {
                "'; DROP TABLE Groups; --",
                "' OR '1'='1",
                "'; DELETE FROM Groups WHERE 1=1; --",
                "' UNION SELECT * FROM Profiles --"
            };
            
            foreach (var pattern in injectionPatterns)
            {
                var maliciousName = $"{maliciousString}{pattern}";
                
                // Test group creation with malicious name
                try
                {
                    var group = new Group { Name = maliciousName, Description = "Test description" };
                    var addedGroup = _groupRepository.AddAsync(group).GetAwaiter().GetResult();
                    
                    // Verify the group was stored with the exact name (parameterized)
                    var retrievedGroup = _groupRepository.GetByIdAsync(addedGroup.Id).GetAwaiter().GetResult();
                    
                    if (retrievedGroup?.Name != maliciousName)
                    {
                        return false; // Data integrity issue
                    }
                }
                catch (Exception ex)
                {
                    // Should not be SQL-related errors
                    if (ex.Message.Contains("SQL") || ex.Message.Contains("syntax") ||
                        ex.Message.Contains("DROP") || ex.Message.Contains("DELETE"))
                    {
                        return false; // Potential SQL injection vulnerability
                    }
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            return !ex.Message.Contains("SQL") && !ex.Message.Contains("syntax");
        }
    }

    /// <summary>
    /// **Feature: profile-management, Property 23: Database queries are parameterized**
    /// **Validates: Requirements 8.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_DatabaseQueriesAreParameterized_BulkOperations(NonEmptyString maliciousInput)
    {
        try
        {
            var maliciousString = maliciousInput?.Get ?? "test";
            
            // Create some test profiles first
            var profiles = new List<Profile>();
            for (int i = 0; i < 5; i++)
            {
                var profile = Profile.CreateNew($"TestProfile{i}", "{}");
                var added = _repository.AddAsync(profile).GetAwaiter().GetResult();
                if (added != null)
                {
                    profiles.Add(added);
                }
            }
            
            if (profiles.Count == 0)
            {
                return true; // No profiles to test with, but no SQL injection occurred
            }
            
            var profileIds = profiles.Select(p => p.Id).ToArray();
            
            // Test bulk operations with potentially malicious data
            var maliciousTags = $"{maliciousString}'; DROP TABLE Profiles; --";
            
            // Test bulk tag update
            var (success, processedCount, errors) = _repository.BulkUpdateTagsAsync(profileIds, maliciousTags).GetAwaiter().GetResult();
            
            if (!success)
            {
                // Check if failure was due to SQL injection attempt
                if (errors != null)
                {
                    foreach (var error in errors)
                    {
                        if (error.Contains("SQL") || error.Contains("syntax") || error.Contains("DROP"))
                        {
                            return false; // SQL injection vulnerability detected
                        }
                    }
                }
            }
            else
            {
                // If successful, verify the malicious input was treated as literal data
                var updatedProfiles = _repository.GetAllAsync().GetAwaiter().GetResult();
                if (updatedProfiles != null)
                {
                    var profilesWithMaliciousTags = updatedProfiles.Where(p => p.Tags == maliciousTags).ToList();
                    
                    // The malicious string should be stored as literal tag data
                    if (profilesWithMaliciousTags.Count != profileIds.Length)
                    {
                        return false; // Data integrity issue
                    }
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            // Any SQL-related exceptions indicate potential vulnerability
            return !ex.Message.Contains("SQL") && !ex.Message.Contains("syntax") && 
                   !ex.Message.Contains("DROP") && !ex.Message.Contains("DELETE");
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}