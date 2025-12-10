namespace ShadowFox.Core.Models;

public static class ProfileExtensions
{
    public static Profile CloneProfile(this Profile source, string newName, string newFingerprintJson)
    {
        return Profile.CreateFromClone(source, newName, newFingerprintJson);
    }

    public static string GenerateCloneName(this Profile source)
    {
        return $"{source.Name} - Copy";
    }

    public static string GenerateCloneNameWithSuffix(this Profile source, int suffix)
    {
        return $"{source.Name} - Copy ({suffix})";
    }

    public static bool HasTag(this Profile profile, string tag)
    {
        if (string.IsNullOrWhiteSpace(profile.Tags) || string.IsNullOrWhiteSpace(tag))
            return false;

        var tags = profile.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(t => t.Trim())
                              .ToArray();
        
        return tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
    }

    public static string[] GetTagsArray(this Profile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Tags))
            return Array.Empty<string>();

        return profile.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.Trim())
                          .Where(t => !string.IsNullOrEmpty(t))
                          .ToArray();
    }

    public static void SetTags(this Profile profile, string[] tags)
    {
        profile.Tags = tags?.Length > 0 ? string.Join(", ", tags.Where(t => !string.IsNullOrWhiteSpace(t))) : null;
        profile.UpdateModified();
    }

    public static bool MatchesSearchQuery(this Profile profile, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        var searchTerm = query.Trim().ToLowerInvariant();
        
        // Search in name
        if (profile.Name.ToLowerInvariant().Contains(searchTerm))
            return true;

        // Search in group name
        if (profile.Group?.Name?.ToLowerInvariant().Contains(searchTerm) == true)
            return true;

        // Search in tags
        if (profile.Tags?.ToLowerInvariant().Contains(searchTerm) == true)
            return true;

        // Search in notes
        if (profile.Notes?.ToLowerInvariant().Contains(searchTerm) == true)
            return true;

        return false;
    }

    public static bool IsNeverUsed(this Profile profile)
    {
        return profile.LastOpenedAt == null;
    }

    public static TimeSpan GetTimeSinceLastUsed(this Profile profile)
    {
        if (profile.LastOpenedAt == null)
            return TimeSpan.MaxValue;

        return DateTime.UtcNow - profile.LastOpenedAt.Value;
    }

    public static string GetUsageStatusText(this Profile profile)
    {
        if (profile.IsNeverUsed())
            return "Never used";

        var timeSinceLastUsed = profile.GetTimeSinceLastUsed();
        
        if (timeSinceLastUsed.TotalDays < 1)
            return "Used today";
        else if (timeSinceLastUsed.TotalDays < 7)
            return $"Used {(int)timeSinceLastUsed.TotalDays} day(s) ago";
        else if (timeSinceLastUsed.TotalDays < 30)
            return $"Used {(int)(timeSinceLastUsed.TotalDays / 7)} week(s) ago";
        else
            return $"Used {(int)(timeSinceLastUsed.TotalDays / 30)} month(s) ago";
    }

    public static bool IsRecentlyUsed(this Profile profile, int days = 7)
    {
        if (profile.LastOpenedAt == null)
            return false;

        return profile.GetTimeSinceLastUsed().TotalDays <= days;
    }

    public static bool IsFrequentlyUsed(this Profile profile, int minimumUsageCount = 10)
    {
        return profile.UsageCount >= minimumUsageCount;
    }
}
