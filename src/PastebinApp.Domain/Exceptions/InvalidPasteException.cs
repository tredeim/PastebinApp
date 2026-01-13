namespace PastebinApp.Domain.Exceptions;

public class InvalidPasteException : DomainException
{
    public InvalidPasteException(string message) : base(message) { }
}