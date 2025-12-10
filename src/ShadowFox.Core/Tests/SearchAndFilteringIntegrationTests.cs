using ShadowFox.Core.Models;
using Xunit;

namespace ShadowFox.Core.Tests;

/// <summary>
/// Integration tests to verify that search and filtering capabilities work correctly
/// </summary>
public class SearchAndFilteringIntegrationTests
{
    [Fact]
    public void SearchAndFiltering_AllCapabilities_WorkCorrectly()
    {
        // Arrange - Create test profiles with different characteristics
        var profiles = new List<Profile>
        {
            Profile.CreateNew("Work Profile 1", "{\"userAgent\":\"test\"}", groupId: 1, tags: "work,development", notes: "Important work profile"),
            Profile.CreateNew("Personal Profile", "{\"userAgent\":\"test\"}", groupId: 2, tags: "personal,social", notes: "Personal browsing"),
            Profile.CreateNew("Testing Profile", "{\"userAgent\":\"test\"}", groupId: 1, tags: "testing,automation", notes: "For testing purposes"),
            Profile.CreateNew("Gaming Profile", "{\"userAgent\":\"test\"}", groupId: 3, tags: "gaming,entertainment", notes: "Gaming and fun"),
        };

        // Set up groups
        var groups = new List<Group>
        {
            new() { Id = 1, Name = "Work" },
            new() { Id = 2, Name = "Personal" },
            new() { Id = 3, Name = "Entertainment" }
        };

        // Assign group navigation properties
        foreach (var profile in profiles)
        {
            if (profile.GroupId.HasValue)
            {
                profile.Group = groups.FirstOrDefault(g => g.Id == profile.GroupId.Value);
            }
        }

        // Test 1: Search functionality - matches across name, group, and tag fields
        var searchFilter = ProfileFilter.BySearchQuery("work");
        var searchResults = profiles.Where(p => searchFilter.MatchesProfile(p)).ToList();
        
        Assert.Equal(2, searchResults.Count); // Should match "Work Profile 1" (name) and profile with "work" tag
        Assert.Contains(searchResults, p => p.Name == "Work Profile 1");
        Assert.Contains(searchResults, p => p.Tags?.Contains("work") == true);

        // Test 2: Group filtering
        var groupFilter = ProfileFilter.ByGroup(1);
        var groupResults = profiles.Where(p => groupFilter.MatchesProfile(p)).ToList();
        
        Assert.Equal(2, groupResults.Count); // Should match profiles in group 1
        Assert.All(groupResults, p => Assert.Equal(1, p.GroupId));

        // Test 3: Tag filtering
        var tagFilter = ProfileFilter.ByTags("testing");
        var tagResults = profiles.Where(p => tagFilter.MatchesProfile(p)).ToList();
        
        Assert.Single(tagResults); // Should match only "Testing Profile"
        Assert.Equal("Testing Profile", tagResults[0].Name);

        // Test 4: Combined filtering (group + tags)
        var combinedFilter = new ProfileFilter 
        { 
            GroupId = 1, 
            Tags = new[] { "development" } 
        };
        var combinedResults = profiles.Where(p => combinedFilter.MatchesProfile(p)).ToList();
        
        Assert.Single(combinedResults); // Should match only "Work Profile 1"
        Assert.Equal("Work Profile 1", combinedResults[0].Name);

        // Test 5: Sorting by name (ascending)
        var sortedByName = profiles.OrderBy(p => p.Name).ToList();
        Assert.Equal("Gaming Profile", sortedByName[0].Name);
        Assert.Equal("Work Profile 1", sortedByName[3].Name);

        // Test 6: Sorting by creation date (descending) - all have same date, so order should be preserved
        var sortedByDate = profiles.OrderByDescending(p => p.CreatedAt).ToList();
        Assert.Equal(4, sortedByDate.Count);

        // Test 7: Pagination simulation
        var paginatedFilter = new ProfileFilter { Skip = 1, Take = 2 };
        var paginatedResults = profiles.Skip(paginatedFilter.Skip ?? 0)
                                     .Take(paginatedFilter.Take ?? int.MaxValue)
                                     .ToList();
        
        Assert.Equal(2, paginatedResults.Count);
        Assert.Equal("Personal Profile", paginatedResults[0].Name);
        Assert.Equal("Testing Profile", paginatedResults[1].Name);
    }

    [Fact]
    public void ProfileFilter_WithSortingAndPagination_WorksCorrectly()
    {
        // Arrange - Create profiles with different dates
        var baseDate = DateTime.UtcNow;
        var profiles = new List<Profile>
        {
            Profile.CreateNew("Profile A", "{\"userAgent\":\"test\"}"),
            Profile.CreateNew("Profile B", "{\"userAgent\":\"test\"}"),
            Profile.CreateNew("Profile C", "{\"userAgent\":\"test\"}"),
            Profile.CreateNew("Profile D", "{\"userAgent\":\"test\"}")
        };

        // Set different creation dates
        profiles[0].CreatedAt = baseDate.AddDays(-3);
        profiles[1].CreatedAt = baseDate.AddDays(-2);
        profiles[2].CreatedAt = baseDate.AddDays(-1);
        profiles[3].CreatedAt = baseDate;

        // Set different usage counts
        profiles[0].UsageCount = 10;
        profiles[1].UsageCount = 5;
        profiles[2].UsageCount = 15;
        profiles[3].UsageCount = 1;

        // Test sorting by creation date (ascending)
        var filter = new ProfileFilter 
        { 
            SortBy = ProfileSortBy.CreatedAt, 
            SortDirection = SortDirection.Ascending 
        };

        var sortedProfiles = ApplySorting(profiles, filter.SortBy, filter.SortDirection).ToList();
        
        Assert.Equal("Profile A", sortedProfiles[0].Name); // Oldest
        Assert.Equal("Profile D", sortedProfiles[3].Name); // Newest

        // Test sorting by usage count (descending)
        filter.SortBy = ProfileSortBy.UsageCount;
        filter.SortDirection = SortDirection.Descending;

        sortedProfiles = ApplySorting(profiles, filter.SortBy, filter.SortDirection).ToList();
        
        Assert.Equal("Profile C", sortedProfiles[0].Name); // Highest usage (15)
        Assert.Equal("Profile D", sortedProfiles[3].Name); // Lowest usage (1)

        // Test pagination with sorting
        var paginatedSorted = ApplySorting(profiles, ProfileSortBy.UsageCount, SortDirection.Descending)
                             .Skip(1)
                             .Take(2)
                             .ToList();
        
        Assert.Equal(2, paginatedSorted.Count);
        Assert.Equal("Profile A", paginatedSorted[0].Name); // Second highest usage (10)
        Assert.Equal("Profile B", paginatedSorted[1].Name); // Third highest usage (5)
    }

    private static IEnumerable<Profile> ApplySorting(List<Profile> profiles, ProfileSortBy sortBy, SortDirection direction)
    {
        return sortBy switch
        {
            ProfileSortBy.Name => direction == SortDirection.Ascending 
                ? profiles.OrderBy(p => p.Name) 
                : profiles.OrderByDescending(p => p.Name),
            ProfileSortBy.CreatedAt => direction == SortDirection.Ascending 
                ? profiles.OrderBy(p => p.CreatedAt) 
                : profiles.OrderByDescending(p => p.CreatedAt),
            ProfileSortBy.LastOpenedAt => direction == SortDirection.Ascending 
                ? profiles.OrderBy(p => p.LastOpenedAt) 
                : profiles.OrderByDescending(p => p.LastOpenedAt),
            ProfileSortBy.UsageCount => direction == SortDirection.Ascending 
                ? profiles.OrderBy(p => p.UsageCount) 
                : profiles.OrderByDescending(p => p.UsageCount),
            _ => profiles
        };
    }
}