using Undefined.Events;
using Undefined.Plugins.Libraries;

namespace Undefined.Plugins.Events.Libraries;

public abstract class LibraryEventArgs : IEventArgs
{
    public abstract ILibrary Library { get; }
}