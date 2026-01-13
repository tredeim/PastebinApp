namespace PastebinApp.Application.Interfaces;

public interface IBlobStorageService
{
    Task UploadContentAsync(
        string hash, 
        string content, 
        CancellationToken cancellationToken = default);
    
    Task<string> GetContentAsync(
        string hash, 
        CancellationToken cancellationToken = default);
    
    Task DeleteContentAsync(
        string hash, 
        CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(
        string hash, 
        CancellationToken cancellationToken = default);
    
    Task<long> GetContentSizeAsync(
        string hash, 
        CancellationToken cancellationToken = default);
}