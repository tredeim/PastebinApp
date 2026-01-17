using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PastebinApp.Application.Interfaces;
using StackExchange.Redis;

namespace PastebinApp.Infrastructure.Services;

public class HashPoolService : IHashPoolService
{
    private readonly IHashPoolRepository _repository;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<HashPoolService> _logger;
    private readonly SemaphoreSlim _refillLock = new(1, 1);
    
    private readonly string _hashPoolKey;
    private readonly int _minPoolSize;
    private readonly int _refillBatchSize;

    public HashPoolService(
        IHashPoolRepository repository,
        IConnectionMultiplexer redis,
        ILogger<HashPoolService> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _redis = redis;
        _logger = logger;
        
        _hashPoolKey = configuration["HashPool:Key"] ?? "hash_pool";
        _minPoolSize = configuration.GetValue<int>("HashPool:MinPoolSize", 500);
        _refillBatchSize = configuration.GetValue<int>("HashPool:RefillBatchSize", 1000);
    }

    public async Task<string> AcquireHashAsync(CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();

        var hash = await db.ListLeftPopAsync(_hashPoolKey);

        if (hash.IsNullOrEmpty)
        {
            _logger.LogWarning("Hash pool is empty, refilling synchronously");
            await RefillPoolAsync(cancellationToken);
            
            hash = await db.ListLeftPopAsync(_hashPoolKey);
            
            if (hash.IsNullOrEmpty)
            {
                throw new InvalidOperationException("Failed to acquire hash from pool");
            }
        }

        var hashValue = hash.ToString();

        var currentSize = await db.ListLengthAsync(_hashPoolKey);
        if (currentSize < _minPoolSize)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await RefillPoolAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background refill failed");
                }
            });
        }

        return hashValue;
    }

    public async Task<int> GetAvailableCountAsync(CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
            var redisCount = await db.ListLengthAsync(_hashPoolKey);
        return (int)redisCount;
    }

    public async Task RefillPoolAsync(CancellationToken cancellationToken = default)
    {   
        if (!await _refillLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogDebug("Refill already in progress, skipping");
            return;
        }

        try
        {
            var currentCount = await GetAvailableCountAsync(cancellationToken);
            
            if (currentCount >= _minPoolSize)
            {
                _logger.LogDebug("Pool has enough hashes ({Count}), skipping refill", currentCount);
                return;
            }
            
            var needCount = _refillBatchSize - currentCount;
            _logger.LogInformation("Refilling hash pool: generating {Count} new hashes", needCount);
            
            var newHashes = await _repository.GenerateBatchAsync(needCount, cancellationToken);
            
            var db = _redis.GetDatabase();
            var hashValues = newHashes.Select(h => (RedisValue)h.Hash).ToArray();
            await db.ListRightPushAsync(_hashPoolKey, hashValues);

            _logger.LogInformation("Hash pool refilled: {Count} hashes added, total: {Total}", 
                needCount, await GetAvailableCountAsync(cancellationToken));
        }
        finally
        {
            _refillLock.Release();
        }
    }
}