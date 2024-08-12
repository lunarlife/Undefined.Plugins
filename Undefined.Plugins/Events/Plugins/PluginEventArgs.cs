using Undefined.Events;

namespace Undefined.Plugins.Events.Plugins;

public abstract class PluginEventArgs<T> : IEventArgs where T : PluginBase<T>
{
    public abstract PluginBase<T> Plugin { get; }
}