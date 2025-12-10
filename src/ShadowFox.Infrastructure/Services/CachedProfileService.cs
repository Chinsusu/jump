using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ShadowFox.Core.Common;
using ShadowFox.Core.Models;
using ShadowFox.Core.Services;

namespace ShadowFox.Infrastructure.Services;

/// <summary>
/// Cached wrapper for ProfileService to improve performance for frequently accessed profiles
/// Implements caching for read operations while maintaining data consistency for write operations
/// </summary>
public class CachedProfileService : IProfileService
{
    private readonly IProfileService _innerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedProfileService> _logger;
    
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ListCacheDuration = TimeSpan.FromMinutes(2);
    
    private const string ProfileCacheKeyPrefix = "profile_";
    private const string ProfileListCacheKey = "profile_list";
    private const string ProfileExistsCacheKeyPrefix = "profile_exists_";

    public CachedProfileService(
        IProfileService innerService, 
        IMemoryCache cache, 
        ILogger<CachedProfileService> logger)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Profile>> CreateAsync(CreateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.CreateAsync(request, cancellationToken);
        
        if (result.IsSuccess)
        {
            // Cache the newly created profile
            var cacheKey = GetProfileCacheKey(result.Value!.Id);
            _cache.Set(cacheKey, result.Value, DefaultCacheDuration);
            
            // Invalidate list cache since we added a new profile
            InvalidateListCache();
            
            _logger.LogDebug("Cached newly created profile {ProfileId}", result.Value.Id);
        }
        
        return result;
    }

    public async Task<Result<Profile>> CloneAsync(int sourceId, string newName, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.CloneAsync(sourceId, newName, cancellationToken);
        
        if (result.IsSuccess)
        {
            // Cache the newly cloned profile
            var cacheKey = GetProfileCacheKey(result.Value!.Id);
            _cache.Set(cacheKey, result.Value, DefaultCacheDuration);
            
            // Invalidate list cache since we added a new profile
            InvalidateListCache();
            
            _logger.LogDebug("Cached newly cloned profile {ProfileId}", result.Value.Id);
        }
        
        return result;
    }

    public async Task<Result<Profile>> UpdateAsync(int id, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.UpdateAsync(id, request, cancellationToken);
        
        if (result.IsSuccess)
        {
            // Update cache with the modified profile
            var cacheKey = GetProfileCacheKey(id);
            _cache.Set(cacheKey, result.Value, DefaultCacheDuration);
            
            // Invalidate list cache since profile data changed
            InvalidateListCache();
            
            _logger.LogDebug("Updated cached profile {ProfileId}", id);
        }
        
        return result;
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.DeleteAsync(id, cancellationToken);
        
        if (result.IsSuccess)
        {
            // Remove from cache
            var cacheKey = GetProfileCacheKey(id);
            _cache.Remove(cacheKey);
            
            // Remove exists cache
            var existsCacheKey = GetProfileExistsCacheKey(id.ToString());
            _cache.Remove(existsCacheKey);
            
            // Invalidate list cache since we removed a profile
            InvalidateListCache();
            
            _logger.LogDebug("Removed profile {ProfileId} from cache", id);
        }
        
        return result;
    }

    public async Task<Result<Profile?>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetProfileCacheKey(id);
        
        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out Profile? cachedProfile))
        {
            _logger.LogDebug("Retrieved profile {ProfileId} from cache", id);
            return Result<Profile?>.Success(cachedProfile);
        }
        
        // Not in cache, get from service
        var result = await _innerService.GetByIdAsync(id, cancellationToken);
        
        if (result.IsSuccess && result.Value != null)
        {
            // Cache the result
            _cache.Set(cacheKey, result.Value, DefaultCacheDuration);
            _logger.LogDebug("Cached profile {ProfileId} from database", id);
        }
        
        return result;
    }

    public async Task<Result<List<Profile>>> GetAllAsync(ProfileFilter? filter = null, CancellationToken cancellationToken = default)
    {
        // Only cache simple list requests without filters for now
        // Complex filtering should go directly to the database for accuracy
        if (filter == null || IsSimpleFilter(filter))
        {
            var cacheKey = GetListCacheKey(filter);
            
            if (_cache.TryGetValue(cacheKey, out List<Profile>? cachedList) && cachedList != null)
            {
                _logger.LogDebug("Retrieved profile list from cache");
                return Result<List<Profile>>.Success(cachedList);
            }
        }
        
        // Get from service
        var result = await _innerService.GetAllAsync(filter, cancellationToken);
        
        if (result.IsSuccess && (filter == null || IsSimpleFilter(filter)))
        {
            // Cache simple results
            var cacheKey = GetListCacheKey(filter);
            _cache.Set(cacheKey, result.Value, ListCacheDuration);
            _logger.LogDebug("Cached profile list from database");
        }
        
        return result;
    }

    public async Task<Result<bool>> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetProfileExistsCacheKey(name);
        
        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out bool cachedExists))
        {
            _logger.LogDebug("Retrieved profile exists check for '{Name}' from cache", name);
            return Result<bool>.Success(cachedExists);
        }
        
        // Not in cache, get from service
        var result = await _innerService.ExistsAsync(name, cancellationToken);
        
        if (result.IsSuccess)
        {
            // Cache the result for a shorter duration
            _cache.Set(cacheKey, result.Value, TimeSpan.FromMinutes(1));
            _logger.LogDebug("Cached profile exists check for '{Name}' from database", name);
        }
        
        return result;
    }

    public async Task<Result<string>> ExportAsync(int[] profileIds, CancellationToken cancellationToken = default)
    {
        // Export operations are not cached as they are typically one-time operations
        return await _innerService.ExportAsync(profileIds, cancellationToken);
    }

    public async Task<Result<ImportResult>> ImportAsync(string jsonData, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.ImportAsync(jsonData, cancellationToken);
        
        if (result.IsSuccess)
        {
            // Invalidate all caches since we imported new profiles
            InvalidateAllCaches();
            _logger.LogDebug("Invalidated all caches after profile import");
        }
        
        return result;
    }

    public async Task<Result<BulkOperationResult>> BulkDeleteAsync(int[] profileIds, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.BulkDeleteAsync(profileIds, cancellationToken);
        
        if (result.IsSuccess)
        {
            // Remove deleted profiles from cache
            foreach (var id in profileIds)
            {
                var cacheKey = GetProfileCacheKey(id);
                _cache.Remove(cacheKey);
                
                var existsCacheKey = GetProfileExistsCacheKey(id.ToString());
                _cache.Remove(existsCacheKey);
            }
            
            // Invalidate list cache
            InvalidateListCache();
            
            _logger.LogDebug("Removed {Count} profiles from cache after bulk delete", profileIds.Length);
        }
        
        return result;
    }

    public async Task<Result<BulkOperationResult>> BulkUpdateTagsAsync(int[] profileIds, string tags, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.BulkUpdateTagsAsync(profileIds, tags, cancellationToken);
        
        if (result.IsSuccess)
        {
            // Invalidate cached profiles that were updated
            foreach (var id in profileIds)
            {
                var cacheKey = GetProfileCacheKey(id);
                _cache.Remove(cacheKey);
            }
            
            // Invalidate list cache since profile data changed
            InvalidateListCache();
            
            _logger.LogDebug("Invalidated {Count} profiles from cache after bulk tag update", profileIds.Length);
        }
        
        return result;
    }

    public async Task<Result<BulkOperationResult>> BulkAssignGroupAsync(int[] profileIds, int? groupId, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.BulkAssignGroupAsync(profileIds, groupId, cancellationToken);
        
        if (result.IsSuccess)
        {
            // Invalidate cached profiles that were updated
            foreach (var id in profileIds)
            {
                var cacheKey = GetProfileCacheKey(id);
                _cache.Remove(cacheKey);
            }
            
            // Invalidate list cache since profile data changed
            InvalidateListCache();
            
            _logger.LogDebug("Invalidated {Count} profiles from cache after bulk group assignment", profileIds.Length);
        }
        
        return result;
    }

    public async Task<Result> RecordProfileAccessAsync(int profileId, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.RecordProfileAccessAsync(profileId, cancellationToken);
        
        if (result.IsSuccess)
        {
            // Invalidate the cached profile since usage data changed
            var cacheKey = GetProfileCacheKey(profileId);
            _cache.Remove(cacheKey);
            
            _logger.LogDebug("Invalidated profile {ProfileId} from cache after access recording", profileId);
        }
        
        return result;
    }

    private static string GetProfileCacheKey(int id) => $"{ProfileCacheKeyPrefix}{id}";
    
    private static string GetProfileExistsCacheKey(string name) => $"{ProfileExistsCacheKeyPrefix}{name}";
    
    private static string GetListCacheKey(ProfileFilter? filter)
    {
        if (filter == null)
            return ProfileListCacheKey;
        
        // Create a simple cache key for basic filters
        return $"{ProfileListCacheKey}_{filter.GroupId}_{filter.IsActive}";
    }
    
    private static bool IsSimpleFilter(ProfileFilter filter)
    {
        // Only cache simple filters to avoid cache complexity
        return string.IsNullOrEmpty(filter.SearchQuery) &&
               (filter.Tags == null || filter.Tags.Length == 0) &&
               !filter.CreatedAfter.HasValue &&
               !filter.CreatedBefore.HasValue &&
               !filter.LastOpenedAfter.HasValue &&
               !filter.LastOpenedBefore.HasValue &&
               !filter.NeverUsed.HasValue &&
               filter.SortBy == ProfileSortBy.CreatedAt &&
               !filter.Skip.HasValue &&
               !filter.Take.HasValue;
    }
    
    private void InvalidateListCache()
    {
        // Remove all list cache entries
        _cache.Remove(ProfileListCacheKey);
        
        // Remove filtered list cache entries (simple approach - remove known patterns)
        for (int groupId = 0; groupId <= 100; groupId++) // Assume max 100 groups
        {
            _cache.Remove($"{ProfileListCacheKey}_{groupId}_True");
            _cache.Remove($"{ProfileListCacheKey}_{groupId}_False");
            _cache.Remove($"{ProfileListCacheKey}_{groupId}_");
        }
    }
    
    private void InvalidateAllCaches()
    {
        // This is a simple approach - in production you might want a more sophisticated cache invalidation strategy
        if (_cache is MemoryCache memoryCache)
        {
            // Clear all cache entries (this is a bit aggressive but ensures consistency)
            var field = typeof(MemoryCache).GetField("_coherentState", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field?.GetValue(memoryCache) is object coherentState)
            {
                var entriesCollection = coherentState.GetType()
                    .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (entriesCollection?.GetValue(coherentState) is System.Collections.IDictionary entries)
                {
                    var keysToRemove = new List<object>();
                    foreach (System.Collections.DictionaryEntry entry in entries)
                    {
                        if (entry.Key.ToString()?.StartsWith(ProfileCacheKeyPrefix) == true ||
                            entry.Key.ToString()?.StartsWith(ProfileListCacheKey) == true ||
                            entry.Key.ToString()?.StartsWith(ProfileExistsCacheKeyPrefix) == true)
                        {
                            keysToRemove.Add(entry.Key);
                        }
                    }
                    
                    foreach (var key in keysToRemove)
                    {
                        _cache.Remove(key);
                    }
                }
            }
        }
    }
}