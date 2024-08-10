namespace Undefined.Plugins.Exceptions;

public class PluginException : Exception
{
    public PluginException(string? message) : base(message)
    {
    }
}