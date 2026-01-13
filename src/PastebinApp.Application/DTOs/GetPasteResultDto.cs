namespace PastebinApp.Application.DTOs;

public class GetPasteResultDto
{
    public required string Hash { get; init; }

    public required string Content { get; init; }

    public string? Title { get; init; }

    public string? Language { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime ExpiresAt { get; init; }

    public int ViewCount { get; init; }

    public long ContentSizeBytes { get; init; }

    public long ExpiresInSeconds { get; init; }

    public bool IsExpired { get; init; }
}