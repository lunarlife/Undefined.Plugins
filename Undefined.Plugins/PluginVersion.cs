namespace Undefined.Plugins;

public struct PluginVersion
{
    public int Major { get; }
    public int Minor { get; }
    public int Revision { get; }

    public PluginVersion(int major, int minor, int revision)
    {
        Major = major;
        Minor = minor;
        Revision = revision;
    }
}