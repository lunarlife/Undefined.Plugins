using Undefined.Events;

namespace Undefined.Plugins.Events.Plugins;

public abstract class PluginEventArgs : IEventArgs
{
    public abstract PluginBase Plugin { get; }
}