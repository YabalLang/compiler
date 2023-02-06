namespace Yabal.Exceptions;

public class InvalidCodeException : Exception
{
    public InvalidCodeException(string? message, SourceRange? range) : base(message)
    {
        Range = range;
    }

    public SourceRange? Range { get; }
}
