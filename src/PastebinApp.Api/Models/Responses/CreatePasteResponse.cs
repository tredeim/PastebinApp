namespace PastebinApp.Api.Models.Responses;

public class CreatePasteResponse
{
    public required string Hash { get; init; }

    public required string Url { get; init; }
    
    public DateTime CreatedAt { get; init; }
    
    public DateTime ExpiresAt { get; init; }

    public long ExpiresInSeconds { get; init; }
}