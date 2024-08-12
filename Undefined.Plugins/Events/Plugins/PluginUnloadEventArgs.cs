namespace Undefined.Plugins.Events.Plugins;

public class PluginUnloadEventArgs<T> : PluginEventArgs<T> where T : PluginBase<T>
{
    public override PluginBase<T> Plugin { get; }

    public PluginUnloadEventArgs(PluginBase<T> plugin)
    {
        Plugin = plugin;
    }
}