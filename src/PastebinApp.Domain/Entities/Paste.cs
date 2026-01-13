namespace PastebinApp.Domain.Entities;

public class Paste
{
    public Guid Id { get; private set; }
    public string Hash { get; private set; }
    public long ContentSizeBytes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public int ViewCount { get; private set; }
    public string? Title { get; private set; }
    
    public string? Language { get; private set; }

    private Paste()
    {
        Hash = string.Empty;
    }

    public static Paste Create(
        string hash,
        long contentSizeBytes,
        TimeSpan expiresIn,
        string? language = null,
        string? title = null)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Hash cannot be empty", nameof(hash));

        if (contentSizeBytes <= 0)
            throw new ArgumentException("ContentSizeBytes must be positive", nameof(contentSizeBytes));
        
        if (expiresIn <= TimeSpan.Zero)
            throw new ArgumentException("ExpiresIn must be positive", nameof(expiresIn));

        var paste = new Paste
        {
            Id = Guid.NewGuid(),
            Hash = hash,
            ContentSizeBytes = contentSizeBytes,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiresIn),
            ViewCount = 0,
            Language = language,
            Title = title
        };

        return paste;
    }

    public void IncrementViewCount() => ViewCount++;
    
    public bool IsExpired() => ExpiresAt < DateTime.UtcNow;
    
    public TimeSpan GetRemainingTime()
    {
        var remaining = ExpiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}