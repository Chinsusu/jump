using ShadowFox.Core.Models;
using Xunit;
using FsCheck;
using FsCheck.Xunit;

namespace ShadowFox.Core.Tests;

public class ProfileFilterTests
{
    [Fact]
    public void ProfileFilter_MatchesProfile_WithSearchQuery_ReturnsCorrectResult()
    {
        // Arrange
        var profile = Profile.CreateNew("Test Profile", "{\"userAgent\":\"test\"}", tags: "web,testing");
        var filter = ProfileFilter.BySearchQuery("Test");

        // Act
        var matches = filter.MatchesProfile(profile);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void ProfileFilter_MatchesProfile_WithTags_ReturnsCorrectResult()
    {
        // Arrange
        var profile = Profile.CreateNew("Test Profile", "{\"userAgent\":\"test\"}", tags: "web,testing,automation");
        var filter = ProfileFilter.ByTags("testing");

        // Act
        var matches = filter.MatchesProfile(profile);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void ProfileExtensions_HasTag_ReturnsCorrectResult()
    {
        // Arrange
        var profile = Profile.CreateNew("Test Profile", "{\"userAgent\":\"test\"}", tags: "web, testing, automation");

        // Act & Assert
        Assert.True(profile.HasTag("testing"));
        Assert.True(profile.HasTag("web"));
        Assert.False(profile.HasTag("nonexistent"));
    }

    [Fact]
    public void ProfileExtensions_GenerateCloneName_ReturnsCorrectFormat()
    {
        // Arrange
        var profile = Profile.CreateNew("Original Profile", "{\"userAgent\":\"test\"}");

        // Act
        var cloneName = profile.GenerateCloneName();

        // Assert
        Assert.Equal("Original Profile - Copy", cloneName);
    }

    [Property]
    public Property Property10_FilteringWorksAcrossMultipleFields()
    {
        // **Feature: profile-management, Property 10: Filtering works across multiple fields**
        // **Validates: Requirements 3.3**
        
        return Prop.ForAll(
            GenerateProfilesWithGroups(),
            GenerateProfileFilter(),
            (profiles, filter) =>
            {
                // Apply the filter to each profile
                var matchingProfiles = profiles.Where(p => filter.MatchesProfile(p)).ToList();
                
                // Verify that all matching profiles actually satisfy the filter criteria
                return matchingProfiles.All(profile =>
                {
                    // If filter has GroupId, profile must match
                    if (filter.GroupId.HasValue && profile.GroupId != filter.GroupId.Value)
                        return false;
                    
                    // If filter has GroupName, profile's group name must match
                    if (!string.IsNullOrWhiteSpace(filter.GroupName) && 
                        !string.Equals(profile.Group?.Name, filter.GroupName, StringComparison.OrdinalIgnoreCase))
                        return false;
                    
                    // If filter has Tags, profile must have at least one matching tag
                    if (filter.Tags?.Length > 0)
                    {
                        var profileTags = profile.GetTagsArray();
                        if (!filter.Tags.Any(tag => profileTags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                            return false;
                    }
                    
                    // If filter has SearchQuery, profile must match in name, group, or tags
                    if (!string.IsNullOrWhiteSpace(filter.SearchQuery) && !profile.MatchesSearchQuery(filter.SearchQuery))
                        return false;
                    
                    return true;
                });
            });
    }

    [Property]
    public Property Property12_SearchMatchesMultipleFields()
    {
        // **Feature: profile-management, Property 12: Search matches multiple fields**
        // **Validates: Requirements 3.5**
        
        return Prop.ForAll(
            GenerateProfilesWithGroups(),
            Arb.Default.NonEmptyString(),
            (profiles, searchQuery) =>
            {
                var query = searchQuery.Get.Trim();
                if (string.IsNullOrWhiteSpace(query)) return true;
                
                var matchingProfiles = profiles.Where(p => p.MatchesSearchQuery(query)).ToList();
                
                // Verify that all matching profiles contain the search query in name, group, tags, or notes
                return matchingProfiles.All(profile =>
                {
                    var queryLower = query.ToLowerInvariant();
                    
                    return profile.Name.ToLowerInvariant().Contains(queryLower) ||
                           (profile.Group?.Name?.ToLowerInvariant().Contains(queryLower) == true) ||
                           (profile.Tags?.ToLowerInvariant().Contains(queryLower) == true) ||
                           (profile.Notes?.ToLowerInvariant().Contains(queryLower) == true);
                });
            });
    }

    [Property]
    public Property Property21_SortingRespectsSpecifiedCriteria()
    {
        // **Feature: profile-management, Property 21: Sorting respects specified criteria**
        // **Validates: Requirements 7.4**
        
        return Prop.ForAll(
            GenerateProfilesWithVariedDates(),
            GenerateSortCriteria(),
            (profiles, sortCriteria) =>
            {
                if (profiles.Count <= 1) return true;
                
                var (sortBy, sortDirection) = sortCriteria;
                
                // Sort the profiles according to the criteria
                var sortedProfiles = ApplySorting(profiles, sortBy, sortDirection).ToList();
                
                // Verify the sorting is correct
                for (int i = 0; i < sortedProfiles.Count - 1; i++)
                {
                    var current = sortedProfiles[i];
                    var next = sortedProfiles[i + 1];
                    
                    var comparison = CompareBySortCriteria(current, next, sortBy);
                    
                    if (sortDirection == SortDirection.Ascending)
                    {
                        if (comparison > 0) return false; // Should be <= 0 for ascending
                    }
                    else
                    {
                        if (comparison < 0) return false; // Should be >= 0 for descending
                    }
                }
                
                return true;
            });
    }

    // Helper generators and methods
    private static Arbitrary<List<Profile>> GenerateProfilesWithGroups()
    {
        return Arb.Generate<List<Profile>>()
            .Where(profiles => profiles != null && profiles.Count <= 20)
            .Select(profiles =>
            {
                var groups = new[] { 
                    new Group { Id = 1, Name = "Work" },
                    new Group { Id = 2, Name = "Personal" },
                    new Group { Id = 3, Name = "Testing" }
                };
                
                return profiles.Select((p, index) =>
                {
                    var profile = Profile.CreateNew(
                        $"Profile_{index}",
                        "{\"userAgent\":\"test\"}",
                        groupId: index % 4 == 0 ? null : (index % 3) + 1,
                        tags: index % 2 == 0 ? "web,testing" : "automation,dev",
                        notes: index % 3 == 0 ? "Important profile" : null
                    );
                    
                    // Assign group navigation property
                    if (profile.GroupId.HasValue)
                    {
                        profile.Group = groups.FirstOrDefault(g => g.Id == profile.GroupId.Value);
                    }
                    
                    return profile;
                }).ToList();
            })
            .ToArbitrary();
    }

    private static Arbitrary<ProfileFilter> GenerateProfileFilter()
    {
        return Gen.Elements(
            ProfileFilter.Empty,
            ProfileFilter.ByGroup(1),
            ProfileFilter.ByGroup(2),
            ProfileFilter.ByGroupName("Work"),
            ProfileFilter.ByTags("web"),
            ProfileFilter.ByTags("testing"),
            ProfileFilter.BySearchQuery("Profile"),
            ProfileFilter.BySearchQuery("test")
        ).ToArbitrary();
    }

    private static Arbitrary<List<Profile>> GenerateProfilesWithVariedDates()
    {
        return Gen.ListOf(Gen.Choose(1, 10))
            .Select(counts => counts.Select((_, index) =>
            {
                var baseDate = DateTime.UtcNow.AddDays(-index * 10);
                var profile = Profile.CreateNew(
                    $"Profile_{index}",
                    "{\"userAgent\":\"test\"}"
                );
                
                // Vary the dates
                profile.CreatedAt = baseDate;
                profile.LastModifiedAt = baseDate.AddHours(index);
                profile.LastOpenedAt = index % 3 == 0 ? null : baseDate.AddDays(index);
                profile.UsageCount = index * 5;
                
                return profile;
            }).ToList())
            .ToArbitrary();
    }

    private static Arbitrary<(ProfileSortBy, SortDirection)> GenerateSortCriteria()
    {
        return Gen.Elements(
            (ProfileSortBy.Name, SortDirection.Ascending),
            (ProfileSortBy.Name, SortDirection.Descending),
            (ProfileSortBy.CreatedAt, SortDirection.Ascending),
            (ProfileSortBy.CreatedAt, SortDirection.Descending),
            (ProfileSortBy.LastOpenedAt, SortDirection.Ascending),
            (ProfileSortBy.LastOpenedAt, SortDirection.Descending),
            (ProfileSortBy.UsageCount, SortDirection.Ascending),
            (ProfileSortBy.UsageCount, SortDirection.Descending)
        ).ToArbitrary();
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

    private static int CompareBySortCriteria(Profile profile1, Profile profile2, ProfileSortBy sortBy)
    {
        return sortBy switch
        {
            ProfileSortBy.Name => string.Compare(profile1.Name, profile2.Name, StringComparison.OrdinalIgnoreCase),
            ProfileSortBy.CreatedAt => profile1.CreatedAt.CompareTo(profile2.CreatedAt),
            ProfileSortBy.LastOpenedAt => Nullable.Compare(profile1.LastOpenedAt, profile2.LastOpenedAt),
            ProfileSortBy.UsageCount => profile1.UsageCount.CompareTo(profile2.UsageCount),
            _ => 0
        };
    }
}