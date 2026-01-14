namespace PastebinApp.Application.Interfaces;

public interface IHashPoolService
{
    Task<string> AcquireHashAsync(CancellationToken cancellationToken = default);
    
    Task<int> GetAvailableCountAsync(CancellationToken cancellationToken = default);
    
    Task RefillPoolAsync(CancellationToken cancellationToken = default);
}