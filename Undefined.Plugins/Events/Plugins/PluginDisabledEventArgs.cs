namespace Undefined.Plugins.Events.Plugins;

public class PluginDisabledEventArgs<T> : PluginEventArgs<T> where T : PluginBase<T>
{
    public override PluginBase<T> Plugin { get; }

    public PluginDisabledEventArgs(PluginBase<T> plugin)
    {
        Plugin = plugin;
    }
}