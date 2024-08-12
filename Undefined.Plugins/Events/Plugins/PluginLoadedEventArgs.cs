namespace Undefined.Plugins.Events.Plugins;

public class PluginLoadedEventArgs<T> : PluginEventArgs<T> where T : PluginBase<T>
{
    public override PluginBase<T> Plugin { get; }

    public PluginLoadedEventArgs(PluginBase<T> plugin)
    {
        Plugin = plugin;
    }
}