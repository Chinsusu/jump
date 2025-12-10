using Microsoft.EntityFrameworkCore;
using ShadowFox.Core.Models;
using ShadowFox.Core.Repositories;
using ShadowFox.Infrastructure.Data;

namespace ShadowFox.Infrastructure.Repositories;

public sealed class UsageSessionRepository : IUsageSessionRepository
{
    private readonly AppDbContext _db;

    public UsageSessionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UsageSession> AddAsync(UsageSession session, CancellationToken cancellationToken = default)
    {
        _db.UsageSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task UpdateAsync(UsageSession session, CancellationToken cancellationToken = default)
    {
        _db.UsageSessions.Update(session);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<UsageSession>> GetByProfileIdAsync(int profileId, CancellationToken cancellationToken = default)
    {
        return await _db.UsageSessions
            .Where(s => s.ProfileId == profileId)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UsageSession>> GetByProfileIdAsync(int profileId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _db.UsageSessions.Where(s => s.ProfileId == profileId);

        if (fromDate.HasValue)
            query = query.Where(s => s.StartTime >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.StartTime <= toDate.Value);

        return await query
            .OrderByDescending(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<UsageSession?> GetActiveSessionAsync(int profileId, CancellationToken cancellationToken = default)
    {
        return await _db.UsageSessions
            .Where(s => s.ProfileId == profileId && s.EndTime == null)
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteOldSessionsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var oldSessions = await _db.UsageSessions
            .Where(s => s.StartTime < cutoffDate)
            .ToListAsync(cancellationToken);

        if (oldSessions.Count > 0)
        {
            _db.UsageSessions.RemoveRange(oldSessions);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<UsageSession>> GetSessionsInRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _db.UsageSessions
            .Where(s => s.StartTime >= fromDate && s.StartTime <= toDate)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }
}