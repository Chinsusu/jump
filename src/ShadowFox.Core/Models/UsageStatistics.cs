namespace ShadowFox.Core.Models;

/// <summary>
/// Represents usage statistics for a profile
/// </summary>
public class UsageStatistics
{
    public int ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public DateTime? FirstUsed { get; set; }
    public DateTime? LastUsed { get; set; }
    public TimeSpan TotalUsageTime { get; set; }
    public TimeSpan AverageSessionTime { get; set; }
    public int DaysSinceLastUsed { get; set; }
    public bool IsNeverUsed { get; set; }
    
    public static UsageStatistics FromProfile(Profile profile)
    {
        var stats = new UsageStatistics
        {
            ProfileId = profile.Id,
            ProfileName = profile.Name,
            TotalSessions = profile.UsageCount,
            LastUsed = profile.LastOpenedAt,
            IsNeverUsed = profile.IsNeverUsed()
        };
        
        if (profile.LastOpenedAt.HasValue)
        {
            stats.DaysSinceLastUsed = (int)(DateTime.UtcNow - profile.LastOpenedAt.Value).TotalDays;
        }
        else
        {
            stats.DaysSinceLastUsed = int.MaxValue;
        }
        
        return stats;
    }
}

/// <summary>
/// Represents a usage session for audit logging
/// </summary>
public class UsageSession
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? SessionNotes { get; set; }
    
    // Navigation property
    public Profile? Profile { get; set; }
    
    public static UsageSession StartNew(int profileId, string? userAgent = null, string? ipAddress = null)
    {
        return new UsageSession
        {
            ProfileId = profileId,
            StartTime = DateTime.UtcNow,
            UserAgent = userAgent,
            IpAddress = ipAddress
        };
    }
    
    public void EndSession()
    {
        EndTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Aggregated usage statistics for reporting
/// </summary>
public class UsageReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ReportPeriod { get; set; }
    public int TotalProfiles { get; set; }
    public int ActiveProfiles { get; set; }
    public int NeverUsedProfiles { get; set; }
    public int TotalSessions { get; set; }
    public TimeSpan TotalUsageTime { get; set; }
    public TimeSpan AverageSessionTime { get; set; }
    public List<UsageStatistics> TopUsedProfiles { get; set; } = new();
    public List<UsageStatistics> RecentlyUsedProfiles { get; set; } = new();
    public List<UsageStatistics> NeverUsedProfilesList { get; set; } = new();
    
    public double ActiveProfilePercentage => TotalProfiles > 0 ? (double)ActiveProfiles / TotalProfiles * 100 : 0;
    public double NeverUsedPercentage => TotalProfiles > 0 ? (double)NeverUsedProfiles / TotalProfiles * 100 : 0;
}