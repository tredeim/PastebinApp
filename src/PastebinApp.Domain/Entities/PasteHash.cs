namespace PastebinApp.Domain.Entities;

public class PasteHash
{
    public long Id { get; private set; }
    public string Hash { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    private PasteHash()
    {
        Hash = string.Empty;
    }

    public static PasteHash Create(long id)
    {
        var hash = ConvertToBase62(id);
        
        return new PasteHash
        {
            Id = id,
            Hash = hash,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }

    private static string ConvertToBase62(long value)
    {
        const string base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        const int targetLength = 8;

        if (value == 0) return new string('0', targetLength);

        var result = new List<char>();
        while (value > 0)
        {
            result.Insert(0, base62Chars[(int)(value % 62)]);
            value /= 62;
        }

        while (result.Count < targetLength)
        {
            result.Insert(0, '0');
        }

        return new string(result.ToArray());
    }
}