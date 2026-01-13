namespace PastebinApp.Domain.Exceptions;

public class PasteNotFoundException : DomainException
{
    public string Hash { get; }

    public PasteNotFoundException(string hash)
        : base($"Paste with hash '{hash}' not found")
    {
        Hash = hash;
    }
}