using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using PastebinApp.Application.Interfaces;

namespace PastebinApp.Infrastructure.BackgroundServices;

public class ExpiredPastesCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredPastesCleanupBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    
    private readonly int _cleanupIntervalMinutes;
    private readonly int _batchSize;

    public ExpiredPastesCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ExpiredPastesCleanupBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        _cleanupIntervalMinutes = _configuration.GetValue("Cleanup:IntervalMinutes", 60);
        _batchSize = _configuration.GetValue("Cleanup:BatchSize", 100);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Expired Pastes Cleanup Background Service started. Interval: {Interval} minutes, Batch Size: {BatchSize}",
            _cleanupIntervalMinutes, _batchSize);

        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during expired pastes cleanup");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_cleanupIntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Expired Pastes Cleanup Background Service stopped");
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPasteRepository>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var blobStorageService = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();

        var totalDeleted = 0;
        var batchNumber = 0;

        _logger.LogInformation("Starting expired pastes cleanup");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            batchNumber++;
            var expiredPastes = await repository.GetExpiredPastesAsync(_batchSize, cancellationToken);

            if (expiredPastes.Count == 0)
            {
                _logger.LogDebug("No expired pastes found in batch {BatchNumber}", batchNumber);
                break;
            }

            _logger.LogInformation(
                "Processing batch {BatchNumber}: {Count} expired pastes found",
                batchNumber, expiredPastes.Count);

            var deletedInBatch = 0;

            foreach (var paste in expiredPastes)
            {
                try
                {
                    await DeletePasteFromAllStoragesAsync(
                        paste.Hash,
                        repository,
                        cacheService,
                        blobStorageService,
                        cancellationToken);

                    deletedInBatch++;
                    totalDeleted++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to delete expired paste {Hash}. Error: {ErrorMessage}",
                        paste.Hash, ex.Message);
                }
            }

            _logger.LogInformation(
                "Batch {BatchNumber} completed: {Deleted} pastes deleted",
                batchNumber, deletedInBatch);
            
            if (expiredPastes.Count < _batchSize)
            {
                break;
            }
            
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        if (totalDeleted > 0)
        {
            _logger.LogInformation(
                "Cleanup completed. Total expired pastes deleted: {TotalDeleted} in {Batches} batches",
                totalDeleted, batchNumber);
        }
        else
        {
            _logger.LogDebug("Cleanup completed. No expired pastes found");
        }
    }

    private async Task DeletePasteFromAllStoragesAsync(
        string hash,
        IPasteRepository repository,
        ICacheService cacheService,
        IBlobStorageService blobStorageService,
        CancellationToken cancellationToken)
    {
        // Remove from cache (Redis)
        try
        {
            await cacheService.RemovePasteAsync(hash, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove paste {Hash} from cache", hash);
        }

        // Remove contents from blob storage (MinIO)
        try
        {
            await blobStorageService.DeleteContentAsync(hash, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete paste {Hash} content from blob storage", hash);
        }

        // Remove contents from DB (PostgreSQL)
        try
        {
            var paste = await repository.GetByHashAsync(hash, cancellationToken);
            if (paste != null)
            {
                await repository.DeleteAsync(paste, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete paste {Hash} from database", hash);
            throw;
        }
    }
}
