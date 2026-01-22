using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PastebinApp.Application.Interfaces;
using PastebinApp.Domain.Entities;

namespace PastebinApp.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly string _cacheKeyPrefix;
    private readonly string _viewCountPrefix;
    private readonly TimeSpan _viewCountSlidingExpiration;

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger,
        IConfiguration configuration)
    {
        _cache = cache;
        _logger = logger;
        
        _cacheKeyPrefix = configuration["Cache:KeyPrefix"] ?? "paste:";
        _viewCountPrefix = configuration["Cache:ViewCountPrefix"] ?? "views:";
        var viewCountExpirationHours = configuration.GetValue<int>("Cache:ViewCountSlidingExpirationHours", 1);
        _viewCountSlidingExpiration = TimeSpan.FromHours(viewCountExpirationHours);
    }

    public async Task<Paste?> GetPasteAsync(string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCacheKey(hash);
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);

            if (cachedData == null)
            {
                _logger.LogDebug("Cache miss for hash: {Hash}", hash);
                return null;
            }

            _logger.LogDebug("Cache hit for hash: {Hash}", hash);
            return JsonSerializer.Deserialize<Paste>(cachedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paste from cache: {Hash}", hash);
            return null;
        }
    }

    public async Task SetPasteAsync(Paste paste, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCacheKey(paste.Hash);
            var serialized = JsonSerializer.Serialize(paste);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = paste.ExpiresAt
            };

            await _cache.SetStringAsync(key, serialized, options, cancellationToken);
            _logger.LogDebug("Cached paste: {Hash}, expires at: {ExpiresAt}", paste.Hash, paste.ExpiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching paste: {Hash}", paste.Hash);
        }
    }

    public async Task RemovePasteAsync(string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCacheKey(hash);
            await _cache.RemoveAsync(key, cancellationToken);
            
            var viewKey = GetViewCountKey(hash);
            await _cache.RemoveAsync(viewKey, cancellationToken);
            
            _logger.LogDebug("Removed from cache: {Hash}", hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing paste from cache: {Hash}", hash);
        }
    }

    public async Task<bool> ExistsAsync(string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCacheKey(hash);
            var exists = await _cache.GetStringAsync(key, cancellationToken);
            return exists != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence: {Hash}", hash);
            return false;
        }
    }

    public async Task IncrementViewCountAsync(string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetViewCountKey(hash);
            var currentValue = await _cache.GetStringAsync(key, cancellationToken);
            
            var newValue = currentValue != null 
                ? int.Parse(currentValue) + 1 
                : 1;

            await _cache.SetStringAsync(
                key, 
                newValue.ToString(), 
                new DistributedCacheEntryOptions 
                { 
                    SlidingExpiration = _viewCountSlidingExpiration
                },
                cancellationToken);

            _logger.LogDebug("Incremented view count for {Hash}: {ViewCount}", hash, newValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing view count: {Hash}", hash);
        }
    }

    public async Task<int> GetViewCountAsync(string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetViewCountKey(hash);
            var value = await _cache.GetStringAsync(key, cancellationToken);
            return value != null ? int.Parse(value) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting view count: {Hash}", hash);
            return 0;
        }
    }

    private string GetCacheKey(string hash) => $"{_cacheKeyPrefix}{hash}";
    private string GetViewCountKey(string hash) => $"{_viewCountPrefix}{hash}";
}