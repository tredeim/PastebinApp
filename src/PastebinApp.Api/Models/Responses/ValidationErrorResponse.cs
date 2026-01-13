namespace PastebinApp.Api.Models.Responses;

public class ValidationErrorResponse
{
    public string Message { get; init; } = "Validation failed";
    
    public required IEnumerable<ValidationError> Errors { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public class ValidationError
{
    public required string Field { get; init; }

    public required string Message { get; init; }
}