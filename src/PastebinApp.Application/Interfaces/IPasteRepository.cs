using PastebinApp.Domain.Entities;

namespace PastebinApp.Application.Interfaces;

public interface IPasteRepository
{
    Task<Paste?> GetByHashAsync(string hash, CancellationToken cancellationToken = default);

    Task<Paste?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Paste> AddAsync(Paste paste, CancellationToken cancellationToken = default);

    Task UpdateAsync(Paste paste, CancellationToken cancellationToken = default);

    Task DeleteAsync(Paste paste, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string hash, CancellationToken cancellationToken = default);

    Task<List<Paste>> GetExpiredPastesAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    Task<int> DeleteExpiredPastesAsync(CancellationToken cancellationToken = default);
}