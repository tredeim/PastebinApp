using PastebinApp.Domain.Entities;

namespace PastebinApp.Domain.Services;

public class PasteHashGenerator
{
    private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int DefaultHashLength = 8;
    
    public PasteHash Generate(int length = DefaultHashLength)
    {
        if (length < 6 || length > 12)
            throw new ArgumentException("Hash length must be between 6 and 12", nameof(length));
        
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int randomPart = Random.Shared.Next();
        
        long combined = (timestamp << 32) | (uint)randomPart;
        
        string hash = ToBase62(combined, length);
        
        return PasteHash.Create(hash);
    }

    public List<PasteHash> GenerateBatch(int count, int length = DefaultHashLength)
    {
        var hashes = new HashSet<PasteHash>();
        
        while (hashes.Count < count)
        {
            hashes.Add(Generate(length));
            
            if (hashes.Count < count)
                Task.Delay(1);
        }
        
        return hashes.ToList();
    }

    private string ToBase62(long value, int length)
    {
        if (value < 0)
            value = Math.Abs(value);

        var result = new char[length];
        
        for (int i = length - 1; i >= 0; i--)
        {
            result[i] = Base62Chars[(int)(value % 62)];
            value /= 62;
        }
        
        return new string(result);
    }

    public bool IsValidFormat(string hash)
    {
        return PasteHash.TryCreate(hash, out _);
    }
}