namespace PastebinApp.Domain.Entities;

public sealed class PasteHash : IEquatable<PasteHash>
{
    private const int MinLength = 6;
    private const int MaxLength = 12;
    private const string AllowedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public string Value { get; }

    private PasteHash(string value)
    {
        Value = value;
    }
    
    public static PasteHash Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Hash cannot be empty", nameof(value));

        if (value.Length < MinLength || value.Length > MaxLength)
            throw new ArgumentException(
                $"Hash length must be between {MinLength} and {MaxLength} characters",
                nameof(value));

        if (!IsValidHash(value))
            throw new ArgumentException(
                "Hash can only contain alphanumeric characters (a-z, A-Z, 0-9)",
                nameof(value));

        return new PasteHash(value);
    }

    public static bool TryCreate(string value, out PasteHash? hash)
    {
        try
        {
            hash = Create(value);
            return true;
        }
        catch
        {
            hash = null;
            return false;
        }
    }

    private static bool IsValidHash(string value)
    {
        return value.All(c => AllowedCharacters.Contains(c));
    }
    
    public bool Equals(PasteHash? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PasteHash other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(PasteHash? left, PasteHash? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(PasteHash? left, PasteHash? right)
    {
        return !(left == right);
    }

    public static implicit operator string(PasteHash hash) => hash.Value;
}