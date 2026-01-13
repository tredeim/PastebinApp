using PastebinApp.Domain.Entities;

namespace PastebinApp.Application.Interfaces;

public interface ICacheService
{
    Task<Paste?> GetPasteAsync(string hash, CancellationToken cancellationToken = default);

    Task SetPasteAsync(Paste paste, CancellationToken cancellationToken = default);

    Task RemovePasteAsync(string hash, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string hash, CancellationToken cancellationToken = default);

    Task IncrementViewCountAsync(string hash, CancellationToken cancellationToken = default);

    Task<int> GetViewCountAsync(string hash, CancellationToken cancellationToken = default);
}