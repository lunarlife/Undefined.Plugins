using Undefined.Events;

namespace Undefined.Plugins.Events;

public abstract class PluginEventArgs : IEventArgs
{
    public abstract PluginBase Plugin { get; }
}