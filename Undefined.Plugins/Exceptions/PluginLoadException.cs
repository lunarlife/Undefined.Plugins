namespace Undefined.Plugins.Exceptions;

public class PluginLoadException : PluginException
{
    public PluginLoadException(string? message) : base(message)
    {
    }
}