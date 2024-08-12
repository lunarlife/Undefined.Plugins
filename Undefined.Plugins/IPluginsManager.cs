using Undefined.Plugins.Libraries;

namespace Undefined.Plugins;

public interface IPluginsManager : IDisposable
{
    public IReadOnlyList<LibrariesDirectory> Directories { get; }
    public IReadOnlyList<PluginBase> Plugins { get; }

    public void DisablePlugin(PluginBase plugin);
    public void EnablePlugin(PluginBase plugin);
    public void UnloadPlugin(PluginBase plugin);

    public IPluginLoadResult LoadPlugin(string file, bool enable = true);

    public IReadOnlyList<IPluginLoadResult> LoadPluginWithReferences(string file, bool enable = true);

    public IReadOnlyList<RuntimeLibrary> LoadLibraryWithReferences(string fileName);
    public RuntimeLibrary LoadLibrary(string fileName);

    public IReadOnlyList<IPluginLoadResult> Reload(ReloadType reloadType);

    public IReadOnlyList<PluginBase> GetPlugins(Type type);
    public PluginBase? GetPlugin(Type type);
    public bool TryGetPlugin(Type type, out PluginBase? plugin);
    public bool TryGetPlugin(string pluginName, out PluginBase? plugin);
    public bool TryGetPlugin(ILibrary library, out PluginBase? plugin);

    public bool HasPlugin(Type type);
    public bool HasPlugin(PluginBase plugin);
    public IPluginLoadResult ReloadPlugin(PluginBase plugin);
}