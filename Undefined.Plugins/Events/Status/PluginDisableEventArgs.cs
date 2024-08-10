namespace Undefined.Plugins.Events.Status;

public class PluginDisableEventArgs : PluginEventArgs
{
    public override PluginBase Plugin { get; }

    public PluginDisableEventArgs(PluginBase plugin)
    {
        Plugin = plugin;
    }
}