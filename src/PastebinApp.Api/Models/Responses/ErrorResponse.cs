namespace PastebinApp.Api.Models.Responses;

public class ErrorResponse
{
    public required string Error { get; init; }

    public object? Details { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}