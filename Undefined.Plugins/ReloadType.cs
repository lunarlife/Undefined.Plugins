namespace Undefined.Plugins;

[Flags]
public enum ReloadType
{
    LoadNew = 1 << 0,
    ReloadExisted = 1 << 1
}