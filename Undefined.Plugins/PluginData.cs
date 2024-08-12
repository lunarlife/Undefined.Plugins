using Undefined.Plugins.Libraries;

namespace Undefined.Plugins;

public class PluginData
{
    public ILibrary Library { get; }
    public PluginVersion Version { get; }
    public string Name { get; }
    public bool IsUnloadable { get; }
    public bool IsPossibleReload { get; }

    public PluginData(ILibrary library, PluginVersion version, string name, bool isUnloadable)
    {
        Library = library;
        Version = version;
        Name = name;
        IsUnloadable = isUnloadable;
        IsPossibleReload = library is RuntimeLibrary;
    }

    public override string ToString() => $"{Name}({Version})";
}