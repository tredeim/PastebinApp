using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PastebinApp.Application.Interfaces;

namespace PastebinApp.Infrastructure.BackgroundServices;

public class HashPoolRefillBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HashPoolRefillBackgroundService> _logger;
    private const int CheckIntervalSeconds = 30;

    public HashPoolRefillBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<HashPoolRefillBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hash Pool Refill Background Service started");

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        using var scope = _serviceProvider.CreateScope();
        var hashPoolService = scope.ServiceProvider.GetRequiredService<IHashPoolService>();
        
        try
        {
            await hashPoolService.RefillPoolAsync(stoppingToken);
            _logger.LogInformation("Initial hash pool fill completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform initial hash pool fill");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), stoppingToken);
        
                using var checkScope = _serviceProvider.CreateScope();
                var poolService = checkScope.ServiceProvider.GetRequiredService<IHashPoolService>();
                
                var availableCount = await poolService.GetAvailableCountAsync(stoppingToken);
                _logger.LogDebug("Hash pool check: {Count} hashes available", availableCount);

                if (availableCount < 500)
                {
                    _logger.LogInformation("Hash pool low ({Count}), triggering refill", availableCount);
                    await poolService.RefillPoolAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in hash pool refill background service");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        _logger.LogInformation("Hash Pool Refill Background Service stopped");
    }
}