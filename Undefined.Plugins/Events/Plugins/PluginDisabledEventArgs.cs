namespace Undefined.Plugins.Events.Plugins;

public class PluginDisabledEventArgs : PluginEventArgs
{
    public override PluginBase Plugin { get; }

    public PluginDisabledEventArgs(PluginBase plugin)
    {
        Plugin = plugin;
    }
}