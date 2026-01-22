using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PastebinApp.Application.Interfaces;

namespace PastebinApp.Infrastructure.BackgroundServices;

public class HashPoolRefillBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HashPoolRefillBackgroundService> _logger;
    private readonly int _checkIntervalSeconds;
    private readonly int _minPoolSize;

    public HashPoolRefillBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<HashPoolRefillBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        _checkIntervalSeconds = configuration.GetValue<int>("HashPool:CheckIntervalSeconds", 30);
        _minPoolSize = configuration.GetValue<int>("HashPool:MinPoolSize", 500);
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
                await Task.Delay(TimeSpan.FromSeconds(_checkIntervalSeconds), stoppingToken);
        
                using var checkScope = _serviceProvider.CreateScope();
                var poolService = checkScope.ServiceProvider.GetRequiredService<IHashPoolService>();
                
                var availableCount = await poolService.GetAvailableCountAsync(stoppingToken);
                _logger.LogDebug("Hash pool check: {Count} hashes available", availableCount);

                if (availableCount < _minPoolSize)
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