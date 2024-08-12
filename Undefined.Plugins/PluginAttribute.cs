namespace Undefined.Plugins;

[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string Name { get; }
    public PluginVersion Version { get; }
    public bool IsUnloadable { get; }

    public PluginAttribute(string name, int major, int minor, int revision, bool isUnloadable = true)
    {
        Name = name;
        Version = new PluginVersion(major, minor, revision);
        IsUnloadable = isUnloadable;
    }
}