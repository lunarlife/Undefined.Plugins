namespace Undefined.Plugins.Events.Plugins;

public class PluginEnabledEventArgs<T> : PluginEventArgs<T> where T : PluginBase<T>
{
    public override PluginBase<T> Plugin { get; }

    public PluginEnabledEventArgs(PluginBase<T> plugin)
    {
        Plugin = plugin;
    }
}