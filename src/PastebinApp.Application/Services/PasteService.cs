using PastebinApp.Application.DTOs;
using PastebinApp.Application.Interfaces;
using PastebinApp.Domain.Entities;
using PastebinApp.Domain.Exceptions;
using PastebinApp.Domain.Services;
using Microsoft.Extensions.Logging;

namespace PastebinApp.Application.Services;

public class PasteService : IPasteService
{
    private readonly IPasteRepository _repository;
    private readonly ICacheService _cache;
    private readonly IBlobStorageService _blobStorage;
    private readonly PasteHashGenerator _hashGenerator;
    private readonly ILogger<PasteService> _logger;

    public PasteService(
        IPasteRepository repository,
        ICacheService cache,
        IBlobStorageService blobStorage,
        PasteHashGenerator hashGenerator,
        ILogger<PasteService> logger)
    {
        _repository = repository;
        _cache = cache;
        _blobStorage = blobStorage;
        _hashGenerator = hashGenerator;
        _logger = logger;
    }

    public async Task<CreatePasteResultDto> CreatePasteAsync(
        CreatePasteDto dto,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new paste with expiration: {Hours}h", dto.ExpirationHours);

        var hash = _hashGenerator.Generate();

        var attempts = 0;
        while (await _repository.ExistsAsync(hash.Value, cancellationToken) && attempts < 5)
        {
            hash = _hashGenerator.Generate();
            attempts++;
            _logger.LogWarning("Hash collision detected, regenerating. Attempt: {Attempt}", attempts);
        }

        if (attempts >= 5)
        {
            _logger.LogError("Failed to generate unique hash after 5 attempts");
            throw new InvalidOperationException("Failed to generate unique hash");
        }

        await _blobStorage.UploadContentAsync(hash.Value, dto.Content, cancellationToken);
        var contentSize = await _blobStorage.GetContentSizeAsync(hash.Value, cancellationToken);

        var expiresIn = TimeSpan.FromHours(dto.ExpirationHours);
        var paste = Paste.Create(
            hash: hash.Value,
            contentSizeBytes: contentSize,
            expiresIn: expiresIn,
            language: dto.Language,
            title: dto.Title
        );

        await _repository.AddAsync(paste, cancellationToken);

        await _cache.SetPasteAsync(paste, cancellationToken);

        _logger.LogInformation("Paste created successfully. Hash: {Hash}, ExpiresAt: {ExpiresAt}, ContentSize: {Size} bytes", 
            paste.Hash, paste.ExpiresAt, paste.ContentSizeBytes);

        return new CreatePasteResultDto
        {
            Hash = paste.Hash,
            Url = $"{baseUrl.TrimEnd('/')}/{paste.Hash}",
            CreatedAt = paste.CreatedAt,
            ExpiresAt = paste.ExpiresAt,
            ExpiresInSeconds = (long)paste.GetRemainingTime().TotalSeconds
        };
    }

    public async Task<GetPasteResultDto> GetPasteAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting paste by hash: {Hash}", hash);

        var paste = await _cache.GetPasteAsync(hash, cancellationToken);

        if (paste == null)
        {
            _logger.LogDebug("Cache miss for hash: {Hash}, fetching from database", hash);
            
            paste = await _repository.GetByHashAsync(hash, cancellationToken);

            if (paste == null)
            {
                _logger.LogWarning("Paste not found: {Hash}", hash);
                throw new PasteNotFoundException(hash);
            }

            await _cache.SetPasteAsync(paste, cancellationToken);
        }
        else
        {
            _logger.LogDebug("Cache hit for hash: {Hash}", hash);
        }

        if (paste.IsExpired())
        {
            _logger.LogInformation("Paste expired: {Hash}, ExpiresAt: {ExpiresAt}", hash, paste.ExpiresAt);
            
            await _cache.RemovePasteAsync(hash, cancellationToken);
            await _repository.DeleteAsync(paste, cancellationToken);
            
            throw new PasteExpiredException(hash, paste.ExpiresAt);
        }
        
        paste.IncrementViewCount();
        await _repository.UpdateAsync(paste, cancellationToken);
        await _cache.IncrementViewCountAsync(hash, cancellationToken);
        
        var content = await _blobStorage.GetContentAsync(hash, cancellationToken);

        _logger.LogInformation("Paste retrieved successfully: {Hash}, ViewCount: {ViewCount}", 
            hash, paste.ViewCount);

        return new GetPasteResultDto
        {
            Hash = paste.Hash,
            Content = content,
            Title = paste.Title,
            Language = paste.Language,
            CreatedAt = paste.CreatedAt,
            ExpiresAt = paste.ExpiresAt,
            ViewCount = paste.ViewCount,
            ContentSizeBytes = paste.ContentSizeBytes,
            ExpiresInSeconds = (long)paste.GetRemainingTime().TotalSeconds,
            IsExpired = paste.IsExpired()
        };
    }

    public async Task<bool> DeletePasteAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting paste: {Hash}", hash);

        var paste = await _repository.GetByHashAsync(hash, cancellationToken);

        if (paste == null)
        {
            _logger.LogWarning("Paste not found for deletion: {Hash}", hash);
            return false;
        }

        await _repository.DeleteAsync(paste, cancellationToken);
        await _cache.RemovePasteAsync(hash, cancellationToken);
        await _blobStorage.DeleteContentAsync(hash, cancellationToken);

        _logger.LogInformation("Paste deleted successfully: {Hash}", hash);
        return true;
    }
}