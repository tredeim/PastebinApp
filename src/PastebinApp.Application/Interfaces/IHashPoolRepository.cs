using PastebinApp.Domain.Entities;

namespace PastebinApp.Application.Interfaces;

public interface IHashPoolRepository
{
    Task<List<PasteHash>> GenerateBatchAsync(int count, CancellationToken cancellationToken = default);
    
    Task<PasteHash?> GetUnusedHashAsync(string hash, CancellationToken cancellationToken = default);
    
    Task MarkAsUsedAsync(long id, CancellationToken cancellationToken = default);
    
    Task<int> GetUnusedCountAsync(CancellationToken cancellationToken = default);
    
    Task<long> GetNextSequenceValueAsync(CancellationToken cancellationToken = default);
}