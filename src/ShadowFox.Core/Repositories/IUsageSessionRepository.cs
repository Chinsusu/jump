using ShadowFox.Core.Models;

namespace ShadowFox.Core.Repositories;

/// <summary>
/// Repository interface for managing usage session data
/// </summary>
public interface IUsageSessionRepository
{
    /// <summary>
    /// Adds a new usage session
    /// </summary>
    Task<UsageSession> AddAsync(UsageSession session, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing usage session
    /// </summary>
    Task UpdateAsync(UsageSession session, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all sessions for a specific profile
    /// </summary>
    Task<List<UsageSession>> GetByProfileIdAsync(int profileId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets sessions for a profile within a date range
    /// </summary>
    Task<List<UsageSession>> GetByProfileIdAsync(int profileId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the most recent active session for a profile (session without end time)
    /// </summary>
    Task<UsageSession?> GetActiveSessionAsync(int profileId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes sessions older than the specified date
    /// </summary>
    Task DeleteOldSessionsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all sessions within a date range
    /// </summary>
    Task<List<UsageSession>> GetSessionsInRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}