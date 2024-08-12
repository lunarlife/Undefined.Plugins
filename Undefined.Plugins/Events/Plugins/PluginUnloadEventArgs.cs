namespace Undefined.Plugins.Events.Plugins;

public class PluginUnloadEventArgs : PluginEventArgs
{
    public override PluginBase Plugin { get; }

    public PluginUnloadEventArgs(PluginBase plugin)
    {
        Plugin = plugin;
    }
}