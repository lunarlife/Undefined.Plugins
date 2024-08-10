namespace Undefined.Plugins;

public interface IPluginsManager
{
    public IReadOnlyList<DllDirectory> Directories { get; }
    public IReadOnlyList<PluginBase> Plugins { get; }

    public void DisablePlugin(PluginBase plugin);
    public void EnablePlugin(PluginBase plugin);
    public void UnloadPlugin(PluginBase pluginBase);
    public IPluginLoadResult LoadPlugin(string file);
    public IEnumerable<IPluginLoadResult> LoadPluginWithReferences(string file);
    public void Reload(ReloadType reloadType);
    public Reference LoadLibrary(string file);
    public IEnumerable<Reference> LoadLibraryWithReferences(string file);


}