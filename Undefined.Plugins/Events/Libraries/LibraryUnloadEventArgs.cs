using Undefined.Plugins.Libraries;

namespace Undefined.Plugins.Events.Libraries;

public class LibraryUnloadEventArgs : LibraryEventArgs
{
    public override ILibrary Library { get; }

    public LibraryUnloadEventArgs(ILibrary library)
    {
        Library = library;
    }
}