namespace PastebinApp.Application.Interfaces;

/// <summary>
/// Unit of Work pattern for transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IPasteRepository Pastes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}