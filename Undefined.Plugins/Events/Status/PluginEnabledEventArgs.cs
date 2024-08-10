namespace Undefined.Plugins.Events.Status;

public class PluginEnabledEventArgs : PluginEventArgs
{
    public override PluginBase Plugin { get; }

    public PluginEnabledEventArgs(PluginBase plugin)
    {
        Plugin = plugin;
    }
}