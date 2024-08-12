using System.Reflection;
using System.Runtime.Loader;
using Undefined.Plugins.Libraries;

namespace Undefined.Plugins;

public class UndefinedAssemblyLoadContext : AssemblyLoadContext
{
    public RuntimeLibrary Owner { get; }

    public UndefinedAssemblyLoadContext(RuntimeLibrary owner, string name) : base(name, true)
    {
        Owner = owner;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        foreach (var directory in Owner.Directory.PluginsManager.Directories)
            if (directory.TryGetLibrary(assemblyName.Name!, assemblyName.Version!, out var library))
                return library!.Assembly;
        return null;
    }
}