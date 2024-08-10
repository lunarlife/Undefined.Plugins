namespace Undefined.Plugins.Events.Load;

public class PluginLoadedEventArgs : PluginEventArgs
{
    public override PluginBase Plugin { get; }

    public PluginLoadedEventArgs(PluginBase plugin)
    {
        Plugin = plugin;
    }
}