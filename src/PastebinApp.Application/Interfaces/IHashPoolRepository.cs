using PastebinApp.Domain.Entities;

namespace PastebinApp.Application.Interfaces;

public interface IHashPoolRepository
{
    Task<List<PasteHash>> GenerateBatchAsync(int count, CancellationToken cancellationToken = default);
    
    Task<long> GetNextSequenceValueAsync(CancellationToken cancellationToken = default);
}