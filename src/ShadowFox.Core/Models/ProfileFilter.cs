namespace ShadowFox.Core.Models;

public class ProfileFilter
{
    public string? SearchQuery { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string[]? Tags { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastOpenedAfter { get; set; }
    public DateTime? LastOpenedBefore { get; set; }
    public bool? NeverUsed { get; set; }
    public ProfileSortBy SortBy { get; set; } = ProfileSortBy.CreatedAt;
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
    public int? Skip { get; set; }
    public int? Take { get; set; }

    public static ProfileFilter Empty => new();

    public static ProfileFilter ByGroup(int groupId)
    {
        return new ProfileFilter { GroupId = groupId };
    }

    public static ProfileFilter ByGroupName(string groupName)
    {
        return new ProfileFilter { GroupName = groupName };
    }

    public static ProfileFilter ByTags(params string[] tags)
    {
        return new ProfileFilter { Tags = tags };
    }

    public static ProfileFilter BySearchQuery(string query)
    {
        return new ProfileFilter { SearchQuery = query };
    }

    public static ProfileFilter NeverUsedProfiles()
    {
        return new ProfileFilter { NeverUsed = true };
    }

    public static ProfileFilter RecentlyUsed(int days = 7)
    {
        return new ProfileFilter 
        { 
            LastOpenedAfter = DateTime.UtcNow.AddDays(-days),
            SortBy = ProfileSortBy.LastOpenedAt,
            SortDirection = SortDirection.Descending
        };
    }

    public static ProfileFilter RecentlyCreated(int days = 7)
    {
        return new ProfileFilter 
        { 
            CreatedAfter = DateTime.UtcNow.AddDays(-days),
            SortBy = ProfileSortBy.CreatedAt,
            SortDirection = SortDirection.Descending
        };
    }

    public ProfileFilter WithPagination(int skip, int take)
    {
        Skip = skip;
        Take = take;
        return this;
    }

    public ProfileFilter WithSorting(ProfileSortBy sortBy, SortDirection direction = SortDirection.Ascending)
    {
        SortBy = sortBy;
        SortDirection = direction;
        return this;
    }

    public bool HasFilters()
    {
        return !string.IsNullOrWhiteSpace(SearchQuery) ||
               GroupId.HasValue ||
               !string.IsNullOrWhiteSpace(GroupName) ||
               Tags?.Length > 0 ||
               IsActive.HasValue ||
               CreatedAfter.HasValue ||
               CreatedBefore.HasValue ||
               LastOpenedAfter.HasValue ||
               LastOpenedBefore.HasValue ||
               NeverUsed.HasValue;
    }

    public bool MatchesProfile(Profile profile)
    {
        // Search query filter
        if (!string.IsNullOrWhiteSpace(SearchQuery) && !profile.MatchesSearchQuery(SearchQuery))
            return false;

        // Group ID filter
        if (GroupId.HasValue && profile.GroupId != GroupId.Value)
            return false;

        // Group name filter
        if (!string.IsNullOrWhiteSpace(GroupName) && 
            !string.Equals(profile.Group?.Name, GroupName, StringComparison.OrdinalIgnoreCase))
            return false;

        // Tags filter (profile must have at least one of the specified tags)
        if (Tags?.Length > 0)
        {
            var profileTags = profile.GetTagsArray();
            if (!Tags.Any(tag => profileTags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                return false;
        }

        // Active status filter
        if (IsActive.HasValue && profile.IsActive != IsActive.Value)
            return false;

        // Created date filters
        if (CreatedAfter.HasValue && profile.CreatedAt < CreatedAfter.Value)
            return false;

        if (CreatedBefore.HasValue && profile.CreatedAt > CreatedBefore.Value)
            return false;

        // Last opened date filters
        if (LastOpenedAfter.HasValue)
        {
            if (profile.LastOpenedAt == null || profile.LastOpenedAt < LastOpenedAfter.Value)
                return false;
        }

        if (LastOpenedBefore.HasValue)
        {
            if (profile.LastOpenedAt == null || profile.LastOpenedAt > LastOpenedBefore.Value)
                return false;
        }

        // Never used filter
        if (NeverUsed.HasValue)
        {
            var isNeverUsed = profile.IsNeverUsed();
            if (NeverUsed.Value != isNeverUsed)
                return false;
        }

        return true;
    }
}

public enum ProfileSortBy
{
    Name,
    CreatedAt,
    LastOpenedAt,
    LastModifiedAt,
    UsageCount,
    GroupName
}

public enum SortDirection
{
    Ascending,
    Descending
}