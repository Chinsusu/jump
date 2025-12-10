using ShadowFox.Core.Common;
using ShadowFox.Core.Models;
using ShadowFox.Core.Repositories;

namespace ShadowFox.Core.Services;

public class UsageTrackingService : IUsageTrackingService
{
    private readonly IProfileRepository _profileRepository;
    private readonly IUsageSessionRepository _sessionRepository;

    public UsageTrackingService(IProfileRepository profileRepository, IUsageSessionRepository sessionRepository)
    {
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
    }

    public async Task<Result> RecordProfileAccessAsync(int profileId, string? userAgent = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the profile
            var profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken);
            if (profile == null)
                return Result.Failure($"Profile with ID {profileId} not found.", ErrorCode.NotFound);

            // Update profile usage tracking
            profile.UpdateLastOpened();
            await _profileRepository.UpdateAsync(profile, cancellationToken);

            // Create usage session for audit logging
            var session = UsageSession.StartNew(profileId, userAgent, ipAddress);
            await _sessionRepository.AddAsync(session, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to record profile access: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result> EndProfileSessionAsync(int profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find the most recent active session for this profile
            var activeSession = await _sessionRepository.GetActiveSessionAsync(profileId, cancellationToken);
            if (activeSession != null)
            {
                activeSession.EndSession();
                await _sessionRepository.UpdateAsync(activeSession, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to end profile session: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<UsageStatistics>> GetProfileUsageStatisticsAsync(int profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken);
            if (profile == null)
                return Result<UsageStatistics>.Failure($"Profile with ID {profileId} not found.", ErrorCode.NotFound);

            var stats = UsageStatistics.FromProfile(profile);
            
            // Get session data for more detailed statistics
            var sessions = await _sessionRepository.GetByProfileIdAsync(profileId, cancellationToken);
            if (sessions.Count > 0)
            {
                stats.FirstUsed = sessions.Min(s => s.StartTime);
                var completedSessions = sessions.Where(s => s.Duration.HasValue).ToList();
                if (completedSessions.Count > 0)
                {
                    stats.TotalUsageTime = TimeSpan.FromTicks(completedSessions.Sum(s => s.Duration!.Value.Ticks));
                    stats.AverageSessionTime = TimeSpan.FromTicks(stats.TotalUsageTime.Ticks / completedSessions.Count);
                }
            }

            return Result<UsageStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            return Result<UsageStatistics>.Failure($"Failed to get profile usage statistics: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<List<UsageStatistics>>> GetAllProfileUsageStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var profiles = await _profileRepository.GetAllAsync(cancellationToken);
            var statisticsList = new List<UsageStatistics>();

            foreach (var profile in profiles)
            {
                var stats = UsageStatistics.FromProfile(profile);
                
                // Get session data for more detailed statistics
                var sessions = await _sessionRepository.GetByProfileIdAsync(profile.Id, cancellationToken);
                if (sessions.Count > 0)
                {
                    stats.FirstUsed = sessions.Min(s => s.StartTime);
                    var completedSessions = sessions.Where(s => s.Duration.HasValue).ToList();
                    if (completedSessions.Count > 0)
                    {
                        stats.TotalUsageTime = TimeSpan.FromTicks(completedSessions.Sum(s => s.Duration!.Value.Ticks));
                        stats.AverageSessionTime = TimeSpan.FromTicks(stats.TotalUsageTime.Ticks / completedSessions.Count);
                    }
                }
                
                statisticsList.Add(stats);
            }

            return Result<List<UsageStatistics>>.Success(statisticsList);
        }
        catch (Exception ex)
        {
            return Result<List<UsageStatistics>>.Failure($"Failed to get usage statistics: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<UsageReport>> GenerateUsageReportAsync(TimeSpan? period = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var reportPeriod = period ?? TimeSpan.FromDays(30); // Default to 30 days
            var cutoffDate = DateTime.UtcNow - reportPeriod;

            var allStats = await GetAllProfileUsageStatisticsAsync(cancellationToken);
            if (!allStats.IsSuccess)
                return Result<UsageReport>.Failure(allStats.ErrorMessage, allStats.ErrorCode);

            var statistics = allStats.Value;
            var recentStats = statistics.Where(s => s.LastUsed >= cutoffDate || s.IsNeverUsed).ToList();

            var report = new UsageReport
            {
                ReportPeriod = reportPeriod,
                TotalProfiles = statistics.Count,
                ActiveProfiles = statistics.Count(s => !s.IsNeverUsed),
                NeverUsedProfiles = statistics.Count(s => s.IsNeverUsed),
                TotalSessions = statistics.Sum(s => s.TotalSessions),
                TotalUsageTime = TimeSpan.FromTicks(statistics.Sum(s => s.TotalUsageTime.Ticks)),
                TopUsedProfiles = statistics
                    .Where(s => !s.IsNeverUsed)
                    .OrderByDescending(s => s.TotalSessions)
                    .Take(10)
                    .ToList(),
                RecentlyUsedProfiles = statistics
                    .Where(s => s.LastUsed >= cutoffDate)
                    .OrderByDescending(s => s.LastUsed)
                    .Take(10)
                    .ToList(),
                NeverUsedProfilesList = statistics
                    .Where(s => s.IsNeverUsed)
                    .OrderBy(s => s.ProfileName)
                    .ToList()
            };

            // Calculate average session time across all profiles
            var totalCompletedSessions = statistics.Where(s => s.TotalSessions > 0).ToList();
            if (totalCompletedSessions.Count > 0)
            {
                var totalSessionTime = TimeSpan.FromTicks(totalCompletedSessions.Sum(s => s.TotalUsageTime.Ticks));
                var totalSessionCount = totalCompletedSessions.Sum(s => s.TotalSessions);
                if (totalSessionCount > 0)
                {
                    report.AverageSessionTime = TimeSpan.FromTicks(totalSessionTime.Ticks / totalSessionCount);
                }
            }

            return Result<UsageReport>.Success(report);
        }
        catch (Exception ex)
        {
            return Result<UsageReport>.Failure($"Failed to generate usage report: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<List<UsageStatistics>>> GetTopUsedProfilesAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var allStats = await GetAllProfileUsageStatisticsAsync(cancellationToken);
            if (!allStats.IsSuccess)
                return Result<List<UsageStatistics>>.Failure(allStats.ErrorMessage, allStats.ErrorCode);

            var topProfiles = allStats.Value
                .Where(s => !s.IsNeverUsed)
                .OrderByDescending(s => s.TotalSessions)
                .ThenByDescending(s => s.TotalUsageTime)
                .Take(count)
                .ToList();

            return Result<List<UsageStatistics>>.Success(topProfiles);
        }
        catch (Exception ex)
        {
            return Result<List<UsageStatistics>>.Failure($"Failed to get top used profiles: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<List<UsageStatistics>>> GetRecentlyUsedProfilesAsync(int days = 7, int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var allStats = await GetAllProfileUsageStatisticsAsync(cancellationToken);
            if (!allStats.IsSuccess)
                return Result<List<UsageStatistics>>.Failure(allStats.ErrorMessage, allStats.ErrorCode);

            var recentProfiles = allStats.Value
                .Where(s => s.LastUsed >= cutoffDate)
                .OrderByDescending(s => s.LastUsed)
                .Take(count)
                .ToList();

            return Result<List<UsageStatistics>>.Success(recentProfiles);
        }
        catch (Exception ex)
        {
            return Result<List<UsageStatistics>>.Failure($"Failed to get recently used profiles: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<List<UsageStatistics>>> GetNeverUsedProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allStats = await GetAllProfileUsageStatisticsAsync(cancellationToken);
            if (!allStats.IsSuccess)
                return Result<List<UsageStatistics>>.Failure(allStats.ErrorMessage, allStats.ErrorCode);

            var neverUsedProfiles = allStats.Value
                .Where(s => s.IsNeverUsed)
                .OrderBy(s => s.ProfileName)
                .ToList();

            return Result<List<UsageStatistics>>.Success(neverUsedProfiles);
        }
        catch (Exception ex)
        {
            return Result<List<UsageStatistics>>.Failure($"Failed to get never used profiles: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result<List<UsageSession>>> GetProfileSessionsAsync(int profileId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await _sessionRepository.GetByProfileIdAsync(profileId, fromDate, toDate, cancellationToken);
            return Result<List<UsageSession>>.Success(sessions);
        }
        catch (Exception ex)
        {
            return Result<List<UsageSession>>.Failure($"Failed to get profile sessions: {ex.Message}", ErrorCode.DatabaseError);
        }
    }

    public async Task<Result> CleanupOldSessionDataAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow - retentionPeriod;
            await _sessionRepository.DeleteOldSessionsAsync(cutoffDate, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to cleanup old session data: {ex.Message}", ErrorCode.DatabaseError);
        }
    }
}