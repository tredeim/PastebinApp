using Microsoft.EntityFrameworkCore;
using Npgsql;
using PastebinApp.Application.Interfaces;
using PastebinApp.Domain.Entities;

namespace PastebinApp.Infrastructure.Persistence.Repositories;

public class HashPoolRepository : IHashPoolRepository
{
    private readonly ApplicationDbContext _context;

    public HashPoolRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PasteHash>> GenerateBatchAsync(int count, CancellationToken cancellationToken = default)
    {
        var hashes = new List<PasteHash>();

        for (int i = 0; i < count; i++)
        {
            var nextId = await GetNextSequenceValueAsync(cancellationToken);
            var hash = PasteHash.Create(nextId);
            hashes.Add(hash);
        }

        await _context.PreGeneratedHashes.AddRangeAsync(hashes, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return hashes;
    }

    public async Task<PasteHash?> GetUnusedHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await _context.PreGeneratedHashes
            .Where(h => h.Hash == hash && !h.IsUsed)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task MarkAsUsedAsync(long id, CancellationToken cancellationToken = default)
    {
        await _context.PreGeneratedHashes
            .Where(h => h.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(h => h.IsUsed, true)
                    .SetProperty(h => h.UsedAt, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<int> GetUnusedCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PreGeneratedHashes
            .Where(h => !h.IsUsed)
            .CountAsync(cancellationToken);
    }

    public async Task<long> GetNextSequenceValueAsync(CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT nextval('pre_generated_hashes_id_seq')";
        
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }
}