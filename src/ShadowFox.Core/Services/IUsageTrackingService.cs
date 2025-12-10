using ShadowFox.Core.Common;
using ShadowFox.Core.Models;

namespace ShadowFox.Core.Services;

/// <summary>
/// Service for tracking profile usage and generating statistics
/// </summary>
public interface IUsageTrackingService
{
    /// <summary>
    /// Records that a profile was opened/accessed
    /// </summary>
    Task<Result> RecordProfileAccessAsync(int profileId, string? userAgent = null, string? ipAddress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Records that a profile session has ended
    /// </summary>
    Task<Result> EndProfileSessionAsync(int profileId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets usage statistics for a specific profile
    /// </summary>
    Task<Result<UsageStatistics>> GetProfileUsageStatisticsAsync(int profileId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets usage statistics for all profiles
    /// </summary>
    Task<Result<List<UsageStatistics>>> GetAllProfileUsageStatisticsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a comprehensive usage report
    /// </summary>
    Task<Result<UsageReport>> GenerateUsageReportAsync(TimeSpan? period = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the most frequently used profiles
    /// </summary>
    Task<Result<List<UsageStatistics>>> GetTopUsedProfilesAsync(int count = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets recently used profiles
    /// </summary>
    Task<Result<List<UsageStatistics>>> GetRecentlyUsedProfilesAsync(int days = 7, int count = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets profiles that have never been used
    /// </summary>
    Task<Result<List<UsageStatistics>>> GetNeverUsedProfilesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets usage sessions for a profile (for audit purposes)
    /// </summary>
    Task<Result<List<UsageSession>>> GetProfileSessionsAsync(int profileId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleans up old usage session data
    /// </summary>
    Task<Result> CleanupOldSessionDataAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
}