namespace Undefined.Plugins.Exceptions;

public class PluginUnloadException : Exception
{
    public PluginUnloadException(string? message) : base(message)
    {
    }
}