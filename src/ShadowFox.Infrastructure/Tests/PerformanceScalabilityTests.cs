using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Common;
using ShadowFox.Core.Models;
using ShadowFox.Core.Services;
using ShadowFox.Infrastructure.Data;
using ShadowFox.Infrastructure.Repositories;
using ShadowFox.Infrastructure.Services;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace ShadowFox.Infrastructure.Tests;

/// <summary>
/// Performance tests for scalability with large datasets
/// Tests profile creation, search/filtering, and bulk operations performance
/// </summary>
public class PerformanceScalabilityTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ProfileService _profileService;
    private readonly GroupService _groupService;
    private readonly ProfileRepository _profileRepository;
    private readonly GroupRepository _groupRepository;
    private readonly FingerprintGenerator _fingerprintGenerator;
    private readonly ITestOutputHelper _output;

    public PerformanceScalabilityTests(ITestOutputHelper output)
    {
        _output = output;
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _context = new AppDbContext(options);
        _profileRepository = new ProfileRepository(_context);
        _groupRepository = new GroupRepository(_context);
        _fingerprintGenerator = new FingerprintGenerator();
        _profileService = new ProfileService(_profileRepository, _fingerprintGenerator);
        _groupService = new GroupService(_groupRepository, _profileRepository);
    }

    [Fact]
    public async Task ProfileCreation_LargeDataset_ShouldMaintainPerformance()
    {
        // Arrange
        const int profileCount = 1000;
        var stopwatch = Stopwatch.StartNew();
        var createdProfiles = new List<Profile>();

        // Create groups for variety
        var groups = new List<Group>();
        for (int i = 1; i <= 10; i++)
        {
            var groupResult = await _groupService.CreateAsync($"Performance Group {i}");
            Assert.True(groupResult.IsSuccess);
            groups.Add(groupResult.Value!);
        }

        var groupCreationTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Created {groups.Count} groups in {groupCreationTime}ms");

        // Act - Create large number of profiles
        stopwatch.Restart();
        var random = new Random(42); // Fixed seed for reproducible results

        for (int i = 1; i <= profileCount; i++)
        {
            var group = groups[random.Next(groups.Count)];
            var spoofLevel = (SpoofLevel)random.Next(0, 3);
            
            var createRequest = new CreateProfileRequest
            {
                Name = $"Performance Profile {i:D4}",
                SpoofLevel = spoofLevel,
                GroupId = random.Next(0, 3) == 0 ? null : group.Id, // 33% chance of no group
                Tags = GenerateRandomTags(random, i),
                Notes = $"Performance test profile {i} with spoof level {spoofLevel}"
            };

            var result = await _profileService.CreateAsync(createRequest);
            Assert.True(result.IsSuccess, $"Failed to create profile {i}: {result.ErrorMessage}");
            createdProfiles.Add(result.Value!);

            // Log progress every 100 profiles
            if (i % 100 == 0)
            {
                var elapsed = stopwatch.ElapsedMilliseconds;
                var avgTime = elapsed / (double)i;
                _output.WriteLine($"Created {i} profiles in {elapsed}ms (avg: {avgTime:F2}ms per profile)");
            }
        }

        stopwatch.Stop();
        var totalCreationTime = stopwatch.ElapsedMilliseconds;
        var averageCreationTime = totalCreationTime / (double)profileCount;

        // Assert - Performance benchmarks
        _output.WriteLine($"Created {profileCount} profiles in {totalCreationTime}ms");
        _output.WriteLine($"Average creation time: {averageCreationTime:F2}ms per profile");
        
        // Performance assertions (adjusted for test environments)
        Assert.True(averageCreationTime < 100, $"Average profile creation time ({averageCreationTime:F2}ms) exceeds 100ms threshold");
        Assert.True(totalCreationTime < 60000, $"Total creation time ({totalCreationTime}ms) exceeds 60 second threshold");
        
        // Verify all profiles were created
        var allProfiles = await _profileService.GetAllAsync();
        Assert.True(allProfiles.IsSuccess);
        Assert.Equal(profileCount, allProfiles.Value!.Count);

        // Memory usage check
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memoryUsed = GC.GetTotalMemory(false);
        _output.WriteLine($"Memory usage after creating {profileCount} profiles: {memoryUsed / 1024 / 1024:F2} MB");
    }

    [Fact]
    public async Task SearchAndFiltering_ThousandsOfProfiles_ShouldBeEfficient()
    {
        // Arrange - Create test dataset
        const int profileCount = 2000;
        await CreateLargeTestDataset(profileCount);

        var stopwatch = new Stopwatch();
        
        // Test 1: Search by name pattern
        stopwatch.Start();
        var nameSearchFilter = new ProfileFilter { SearchQuery = "Profile 1" };
        var nameSearchResult = await _profileService.GetAllAsync(nameSearchFilter);
        stopwatch.Stop();
        
        Assert.True(nameSearchResult.IsSuccess);
        var nameSearchTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Name search completed in {nameSearchTime}ms, found {nameSearchResult.Value!.Count} profiles");
        
        // Test 2: Filter by group
        stopwatch.Restart();
        var groupFilter = new ProfileFilter { GroupId = 1 };
        var groupFilterResult = await _profileService.GetAllAsync(groupFilter);
        stopwatch.Stop();
        
        Assert.True(groupFilterResult.IsSuccess);
        var groupFilterTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Group filter completed in {groupFilterTime}ms, found {groupFilterResult.Value!.Count} profiles");
        
        // Test 3: Filter by tags
        stopwatch.Restart();
        var tagFilter = new ProfileFilter { Tags = new[] { "performance" } };
        var tagFilterResult = await _profileService.GetAllAsync(tagFilter);
        stopwatch.Stop();
        
        Assert.True(tagFilterResult.IsSuccess);
        var tagFilterTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Tag filter completed in {tagFilterTime}ms, found {tagFilterResult.Value!.Count} profiles");
        
        // Test 4: Complex combined filter
        stopwatch.Restart();
        var complexFilter = new ProfileFilter 
        { 
            SearchQuery = "test",
            GroupId = 2,
            Tags = new[] { "automated" },
            SortBy = ProfileSortBy.CreatedAt,
            SortDirection = SortDirection.Descending,
            Skip = 10,
            Take = 50
        };
        var complexFilterResult = await _profileService.GetAllAsync(complexFilter);
        stopwatch.Stop();
        
        Assert.True(complexFilterResult.IsSuccess);
        var complexFilterTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Complex filter completed in {complexFilterTime}ms, found {complexFilterResult.Value!.Count} profiles");
        
        // Test 5: Pagination performance
        stopwatch.Restart();
        var paginationResults = new List<List<Profile>>();
        const int pageSize = 100;
        int totalPages = profileCount / pageSize;
        
        for (int page = 0; page < Math.Min(totalPages, 10); page++) // Test first 10 pages
        {
            var paginationFilter = new ProfileFilter 
            { 
                Skip = page * pageSize, 
                Take = pageSize,
                SortBy = ProfileSortBy.Name,
                SortDirection = SortDirection.Ascending
            };
            
            var pageResult = await _profileService.GetAllAsync(paginationFilter);
            Assert.True(pageResult.IsSuccess);
            paginationResults.Add(pageResult.Value!);
        }
        
        stopwatch.Stop();
        var paginationTime = stopwatch.ElapsedMilliseconds;
        var avgPageTime = paginationTime / (double)paginationResults.Count;
        _output.WriteLine($"Pagination test completed in {paginationTime}ms for {paginationResults.Count} pages (avg: {avgPageTime:F2}ms per page)");
        
        // Performance assertions (relaxed for test environments)
        Assert.True(nameSearchTime < 2000, $"Name search time ({nameSearchTime}ms) exceeds 2 second threshold");
        Assert.True(groupFilterTime < 1000, $"Group filter time ({groupFilterTime}ms) exceeds 1 second threshold");
        Assert.True(tagFilterTime < 2000, $"Tag filter time ({tagFilterTime}ms) exceeds 2 second threshold");
        Assert.True(complexFilterTime < 2000, $"Complex filter time ({complexFilterTime}ms) exceeds 2 second threshold");
        Assert.True(avgPageTime < 500, $"Average pagination time ({avgPageTime:F2}ms) exceeds 500ms threshold");
    }

    [Fact]
    public async Task BulkOperations_LargeDatasets_ShouldScaleEfficiently()
    {
        // Arrange - Create test dataset
        const int profileCount = 1500;
        var profiles = await CreateLargeTestDataset(profileCount);
        
        var stopwatch = new Stopwatch();
        
        // Test 1: Bulk tag update performance
        var bulkUpdateIds = profiles.Take(500).Select(p => p.Id).ToArray();
        
        stopwatch.Start();
        var bulkTagResult = await _profileService.BulkUpdateTagsAsync(bulkUpdateIds, "bulk,updated,performance");
        stopwatch.Stop();
        
        Assert.True(bulkTagResult.IsSuccess);
        var bulkTagTime = stopwatch.ElapsedMilliseconds;
        var avgTagUpdateTime = bulkTagTime / (double)bulkUpdateIds.Length;
        _output.WriteLine($"Bulk tag update of {bulkUpdateIds.Length} profiles completed in {bulkTagTime}ms (avg: {avgTagUpdateTime:F2}ms per profile)");
        
        // Test 2: Bulk group assignment performance
        var groupResult = await _groupService.CreateAsync("Bulk Assignment Group");
        Assert.True(groupResult.IsSuccess);
        var bulkGroup = groupResult.Value!;
        
        var bulkGroupIds = profiles.Skip(500).Take(300).Select(p => p.Id).ToArray();
        
        stopwatch.Restart();
        var bulkGroupResult = await _profileService.BulkAssignGroupAsync(bulkGroupIds, bulkGroup.Id);
        stopwatch.Stop();
        
        Assert.True(bulkGroupResult.IsSuccess);
        var bulkGroupTime = stopwatch.ElapsedMilliseconds;
        var avgGroupAssignTime = bulkGroupTime / (double)bulkGroupIds.Length;
        _output.WriteLine($"Bulk group assignment of {bulkGroupIds.Length} profiles completed in {bulkGroupTime}ms (avg: {avgGroupAssignTime:F2}ms per profile)");
        
        // Test 3: Bulk delete performance
        var bulkDeleteIds = profiles.Skip(800).Take(200).Select(p => p.Id).ToArray();
        
        stopwatch.Restart();
        var bulkDeleteResult = await _profileService.BulkDeleteAsync(bulkDeleteIds);
        stopwatch.Stop();
        
        Assert.True(bulkDeleteResult.IsSuccess);
        var bulkDeleteTime = stopwatch.ElapsedMilliseconds;
        var avgDeleteTime = bulkDeleteTime / (double)bulkDeleteIds.Length;
        _output.WriteLine($"Bulk delete of {bulkDeleteIds.Length} profiles completed in {bulkDeleteTime}ms (avg: {avgDeleteTime:F2}ms per profile)");
        
        // Test 4: Large export performance
        var exportIds = profiles.Take(1000).Select(p => p.Id).ToArray();
        
        stopwatch.Restart();
        var exportResult = await _profileService.ExportAsync(exportIds);
        stopwatch.Stop();
        
        Assert.True(exportResult.IsSuccess);
        var exportTime = stopwatch.ElapsedMilliseconds;
        var exportSize = exportResult.Value!.Length;
        _output.WriteLine($"Export of {exportIds.Length} profiles completed in {exportTime}ms, size: {exportSize / 1024:F2} KB");
        
        // Performance assertions (relaxed for test environments)
        Assert.True(avgTagUpdateTime < 50, $"Average bulk tag update time ({avgTagUpdateTime:F2}ms) exceeds 50ms per profile threshold");
        Assert.True(avgGroupAssignTime < 50, $"Average bulk group assignment time ({avgGroupAssignTime:F2}ms) exceeds 50ms per profile threshold");
        Assert.True(avgDeleteTime < 100, $"Average bulk delete time ({avgDeleteTime:F2}ms) exceeds 100ms per profile threshold");
        Assert.True(exportTime < 15000, $"Export time ({exportTime}ms) exceeds 15 second threshold");
        
        // Verify operations completed correctly
        var remainingProfiles = await _profileService.GetAllAsync();
        Assert.True(remainingProfiles.IsSuccess);
        Assert.Equal(profileCount - bulkDeleteIds.Length, remainingProfiles.Value!.Count);
        
        // Memory usage after bulk operations
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memoryUsed = GC.GetTotalMemory(false);
        _output.WriteLine($"Memory usage after bulk operations: {memoryUsed / 1024 / 1024:F2} MB");
    }

    [Fact]
    public async Task DatabaseQuery_OptimizationWithIndexes_ShouldBeEfficient()
    {
        // Arrange - Create dataset with varied access patterns
        const int profileCount = 3000;
        await CreateLargeTestDataset(profileCount);
        
        var stopwatch = new Stopwatch();
        
        // Test 1: Index-optimized name lookup
        stopwatch.Start();
        var nameResults = new List<Profile?>();
        for (int i = 0; i < 100; i++)
        {
            var profileName = $"Performance Profile {i + 1:D4}";
            var result = await _profileRepository.GetByNameAsync(profileName);
            nameResults.Add(result);
        }
        stopwatch.Stop();
        
        var nameLookupTime = stopwatch.ElapsedMilliseconds;
        var avgNameLookupTime = nameLookupTime / 100.0;
        _output.WriteLine($"100 name lookups completed in {nameLookupTime}ms (avg: {avgNameLookupTime:F2}ms per lookup)");
        
        // Test 2: Index-optimized group filtering
        stopwatch.Restart();
        var groupQueries = new List<List<Profile>>();
        for (int groupId = 1; groupId <= 10; groupId++)
        {
            var filter = new ProfileFilter { GroupId = groupId };
            var result = await _profileRepository.GetAllAsync(filter);
            groupQueries.Add(result);
        }
        stopwatch.Stop();
        
        var groupQueryTime = stopwatch.ElapsedMilliseconds;
        var avgGroupQueryTime = groupQueryTime / 10.0;
        _output.WriteLine($"10 group queries completed in {groupQueryTime}ms (avg: {avgGroupQueryTime:F2}ms per query)");
        
        // Test 3: Date range queries (testing CreatedAt index)
        var baseDate = DateTime.UtcNow.AddDays(-30);
        stopwatch.Restart();
        var dateRangeQueries = new List<List<Profile>>();
        
        for (int day = 0; day < 10; day++)
        {
            var startDate = baseDate.AddDays(day);
            var endDate = startDate.AddDays(1);
            
            var dateFilter = new ProfileFilter 
            { 
                CreatedAfter = startDate,
                CreatedBefore = endDate
            };
            var result = await _profileRepository.GetAllAsync(dateFilter);
            dateRangeQueries.Add(result);
        }
        stopwatch.Stop();
        
        var dateQueryTime = stopwatch.ElapsedMilliseconds;
        var avgDateQueryTime = dateQueryTime / 10.0;
        _output.WriteLine($"10 date range queries completed in {dateQueryTime}ms (avg: {avgDateQueryTime:F2}ms per query)");
        
        // Test 4: Complex multi-index query
        stopwatch.Restart();
        var complexQueries = new List<List<Profile>>();
        
        for (int i = 0; i < 20; i++)
        {
            var complexFilter = new ProfileFilter
            {
                GroupId = (i % 5) + 1,
                IsActive = i % 2 == 0,
                SortBy = ProfileSortBy.LastModifiedAt,
                SortDirection = SortDirection.Descending,
                Take = 50
            };
            var result = await _profileRepository.GetAllAsync(complexFilter);
            complexQueries.Add(result);
        }
        stopwatch.Stop();
        
        var complexQueryTime = stopwatch.ElapsedMilliseconds;
        var avgComplexQueryTime = complexQueryTime / 20.0;
        _output.WriteLine($"20 complex queries completed in {complexQueryTime}ms (avg: {avgComplexQueryTime:F2}ms per query)");
        
        // Performance assertions
        Assert.True(avgNameLookupTime < 50, $"Average name lookup time ({avgNameLookupTime:F2}ms) exceeds 50ms threshold");
        Assert.True(avgGroupQueryTime < 100, $"Average group query time ({avgGroupQueryTime:F2}ms) exceeds 100ms threshold");
        Assert.True(avgDateQueryTime < 150, $"Average date query time ({avgDateQueryTime:F2}ms) exceeds 150ms threshold");
        Assert.True(avgComplexQueryTime < 200, $"Average complex query time ({avgComplexQueryTime:F2}ms) exceeds 200ms threshold");
    }

    [Fact]
    public async Task ConcurrentOperations_MultipleUsers_ShouldHandleLoad()
    {
        // Arrange - Create initial dataset
        const int initialProfiles = 500;
        await CreateLargeTestDataset(initialProfiles);
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Simulate concurrent operations from multiple users
        var concurrentTasks = new List<Task>();
        const int concurrentUsers = 10;
        const int operationsPerUser = 20;
        
        for (int user = 0; user < concurrentUsers; user++)
        {
            var userId = user;
            var userTask = Task.Run(async () =>
            {
                var userStopwatch = Stopwatch.StartNew();
                
                for (int op = 0; op < operationsPerUser; op++)
                {
                    try
                    {
                        // Mix of operations: 40% read, 30% create, 20% update, 10% delete
                        var operation = op % 10;
                        
                        if (operation < 4) // Read operations
                        {
                            var filter = new ProfileFilter 
                            { 
                                SearchQuery = $"Profile {userId}",
                                Take = 10 
                            };
                            await _profileService.GetAllAsync(filter);
                        }
                        else if (operation < 7) // Create operations
                        {
                            var createRequest = new CreateProfileRequest
                            {
                                Name = $"Concurrent Profile U{userId}O{op}",
                                SpoofLevel = SpoofLevel.Basic,
                                Tags = $"concurrent,user{userId},operation{op}"
                            };
                            await _profileService.CreateAsync(createRequest);
                        }
                        else if (operation < 9) // Update operations
                        {
                            var allProfiles = await _profileService.GetAllAsync(new ProfileFilter { Take = 100 });
                            if (allProfiles.IsSuccess && allProfiles.Value!.Count > 0)
                            {
                                var randomProfile = allProfiles.Value[op % allProfiles.Value.Count];
                                var updateRequest = new UpdateProfileRequest
                                {
                                    Tags = $"updated,user{userId},operation{op}"
                                };
                                await _profileService.UpdateAsync(randomProfile.Id, updateRequest);
                            }
                        }
                        else // Delete operations
                        {
                            var allProfiles = await _profileService.GetAllAsync(new ProfileFilter { Take = 100 });
                            if (allProfiles.IsSuccess && allProfiles.Value!.Count > initialProfiles / 2)
                            {
                                var randomProfile = allProfiles.Value[op % allProfiles.Value.Count];
                                await _profileService.DeleteAsync(randomProfile.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"User {userId} operation {op} failed: {ex.Message}");
                    }
                }
                
                userStopwatch.Stop();
                _output.WriteLine($"User {userId} completed {operationsPerUser} operations in {userStopwatch.ElapsedMilliseconds}ms");
            });
            
            concurrentTasks.Add(userTask);
        }
        
        // Wait for all concurrent operations to complete
        await Task.WhenAll(concurrentTasks);
        stopwatch.Stop();
        
        var totalTime = stopwatch.ElapsedMilliseconds;
        var totalOperations = concurrentUsers * operationsPerUser;
        var avgOperationTime = totalTime / (double)totalOperations;
        
        _output.WriteLine($"Completed {totalOperations} concurrent operations in {totalTime}ms");
        _output.WriteLine($"Average operation time: {avgOperationTime:F2}ms");
        
        // Verify system integrity after concurrent operations
        var finalProfiles = await _profileService.GetAllAsync();
        Assert.True(finalProfiles.IsSuccess);
        _output.WriteLine($"Final profile count: {finalProfiles.Value!.Count}");
        
        // Performance assertions
        Assert.True(avgOperationTime < 500, $"Average concurrent operation time ({avgOperationTime:F2}ms) exceeds 500ms threshold");
        Assert.True(totalTime < 60000, $"Total concurrent operation time ({totalTime}ms) exceeds 60 second threshold");
        
        // Verify no data corruption occurred
        var profileNames = finalProfiles.Value!.Select(p => p.Name).ToList();
        var duplicateNames = profileNames.GroupBy(n => n).Where(g => g.Count() > 1).ToList();
        Assert.Empty(duplicateNames); // No duplicate names should exist
    }

    private async Task<List<Profile>> CreateLargeTestDataset(int profileCount)
    {
        var profiles = new List<Profile>();
        var random = new Random(42); // Fixed seed for reproducible results
        
        // Create groups
        var groups = new List<Group>();
        for (int i = 1; i <= 10; i++)
        {
            var groupResult = await _groupService.CreateAsync($"Performance Group {i}");
            Assert.True(groupResult.IsSuccess);
            groups.Add(groupResult.Value!);
        }
        
        // Create profiles in batches for better performance
        const int batchSize = 100;
        for (int batch = 0; batch < profileCount; batch += batchSize)
        {
            var batchProfiles = new List<Profile>();
            var currentBatchSize = Math.Min(batchSize, profileCount - batch);
            
            for (int i = 0; i < currentBatchSize; i++)
            {
                var profileIndex = batch + i + 1;
                var group = groups[random.Next(groups.Count)];
                var spoofLevel = (SpoofLevel)random.Next(0, 3);
                
                var createRequest = new CreateProfileRequest
                {
                    Name = $"Performance Profile {profileIndex:D4}",
                    SpoofLevel = spoofLevel,
                    GroupId = random.Next(0, 4) == 0 ? null : group.Id, // 25% chance of no group
                    Tags = GenerateRandomTags(random, profileIndex),
                    Notes = $"Performance test profile {profileIndex}"
                };
                
                var result = await _profileService.CreateAsync(createRequest);
                Assert.True(result.IsSuccess);
                batchProfiles.Add(result.Value!);
            }
            
            profiles.AddRange(batchProfiles);
        }
        
        return profiles;
    }

    private static string GenerateRandomTags(Random random, int profileIndex)
    {
        var tagOptions = new[] 
        { 
            "performance", "test", "automated", "benchmark", "scalability",
            "integration", "load", "stress", "volume", "endurance"
        };
        
        var tagCount = random.Next(1, 4); // 1-3 tags per profile
        var selectedTags = tagOptions.OrderBy(x => random.Next()).Take(tagCount);
        
        return string.Join(",", selectedTags.Concat(new[] { $"profile{profileIndex}" }));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}