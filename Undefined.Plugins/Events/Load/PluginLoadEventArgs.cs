namespace Undefined.Plugins.Events.Load;

public class PluginLoadEventArgs : PluginEventArgs
{
    public override PluginBase Plugin { get; }

    public PluginLoadEventArgs(PluginBase plugin)
    {
        Plugin = plugin;
    }
}