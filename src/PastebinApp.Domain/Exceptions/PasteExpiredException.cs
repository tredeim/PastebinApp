namespace PastebinApp.Domain.Exceptions;

public class PasteExpiredException : DomainException
{
    public string Hash { get; }
    public DateTime ExpiredAt { get; }

    public PasteExpiredException(string hash, DateTime expiredAt)
        : base($"Paste '{hash}' expired at {expiredAt:yyyy-MM-dd HH:mm:ss} UTC")
    {
        Hash = hash;
        ExpiredAt = expiredAt;
    }
}