using Undefined.Plugins.Libraries;

namespace Undefined.Plugins.Events.Libraries;

public class LibraryLoadedEventArgs : LibraryEventArgs
{
    public override ILibrary Library { get; }

    public LibraryLoadedEventArgs(ILibrary library)
    {
        Library = library;
    }
}