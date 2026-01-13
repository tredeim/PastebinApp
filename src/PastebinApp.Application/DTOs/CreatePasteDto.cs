namespace PastebinApp.Application.DTOs;

public class CreatePasteDto
{
    public required string Content { get; init; }

    public int ExpirationHours { get; init; } = 24;

    public string? Language { get; init; }

    public string? Title { get; init; }
}