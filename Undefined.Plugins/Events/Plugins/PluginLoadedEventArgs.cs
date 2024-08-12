namespace Undefined.Plugins.Events.Plugins;

public class PluginLoadedEventArgs : PluginEventArgs
{
    public override PluginBase Plugin { get; }

    public PluginLoadedEventArgs(PluginBase plugin)
    {
        Plugin = plugin;
    }
}