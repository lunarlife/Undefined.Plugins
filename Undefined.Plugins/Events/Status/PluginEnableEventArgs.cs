namespace Undefined.Plugins.Events.Status;

public class PluginEnableEventArgs : PluginEventArgs
{
    public override PluginBase Plugin { get; }

    public PluginEnableEventArgs(PluginBase plugin)
    {
        Plugin = plugin;
    }
}