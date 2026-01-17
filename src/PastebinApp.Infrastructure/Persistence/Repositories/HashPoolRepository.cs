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

        await _context.PasteHashes.AddRangeAsync(hashes, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        return hashes;
    }

    public async Task<long> GetNextSequenceValueAsync(CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT nextval('paste_hashes_id_seq')";
        
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }
}