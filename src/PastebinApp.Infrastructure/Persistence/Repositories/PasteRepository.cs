using Microsoft.EntityFrameworkCore;
using PastebinApp.Application.Interfaces;
using PastebinApp.Domain.Entities;

namespace PastebinApp.Infrastructure.Persistence.Repositories;

public class PasteRepository : IPasteRepository
{
    private readonly ApplicationDbContext _context;

    public PasteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Paste?> GetByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await _context.Pastes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Hash == hash, cancellationToken);
    }

    public async Task<Paste?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Pastes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Paste> AddAsync(Paste paste, CancellationToken cancellationToken = default)
    {
        await _context.Pastes.AddAsync(paste, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return paste;
    }

    public async Task UpdateAsync(Paste paste, CancellationToken cancellationToken = default)
    {
        _context.Pastes.Update(paste);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Paste paste, CancellationToken cancellationToken = default)
    {
        _context.Pastes.Remove(paste);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await _context.Pastes
            .AsNoTracking()
            .AnyAsync(p => p.Hash == hash, cancellationToken);
    }

    public async Task<List<Paste>> GetExpiredPastesAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        return await _context.Pastes
            .AsNoTracking()
            .Where(p => p.ExpiresAt <= now)
            .OrderBy(p => p.ExpiresAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredPastesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        return await _context.Pastes
            .Where(p => p.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }
}