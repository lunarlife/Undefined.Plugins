namespace Undefined.Plugins.Exceptions;

public class LibraryUnloadException : LibraryException
{
    public LibraryUnloadException(string? message) : base(message)
    {
    }
}