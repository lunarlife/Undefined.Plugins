using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Undefined.Events;
using Undefined.Plugins.Events.Load;
using Undefined.Plugins.Events.Status;
using Undefined.Plugins.Exceptions;

namespace Undefined.Plugins;

public class PluginsManager<TBase> : IPluginsManager where TBase : PluginBase
{
    private const string CONTEXT_NAME = "Plugins";

    private readonly FieldInfo _pluginManagerField =
        typeof(PluginBase).GetField("<PluginsManager>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly Event<PluginLoadedEventArgs> _onPluginLoaded = new();
    private readonly Event<PluginEnabledEventArgs> _onPluginEnabled = new();
    private readonly Event<PluginDisabledEventArgs> _onPluginDisabled = new();

    private readonly Type _pluginBase;
    private readonly PluginUpdater _pluginUpdater;
    private readonly DllDirectory _pluginsDirectory;
    private readonly DllDirectory _librariesDirectory;

    private AssemblyLoadContext? _context;
    private Dictionary<Type, TBase> _pluginsTypes = [];
    private List<TBase> _plugins = [];

    public IReadOnlyList<DllDirectory> Directories { get; }
    public IReadOnlyList<TBase> Plugins => _plugins.AsReadOnly();

    IReadOnlyList<PluginBase> IPluginsManager.Plugins => Plugins;

    public IEventAccess<PluginLoadedEventArgs> OnPluginLoaded => _onPluginLoaded.Access;
    public IEventAccess<PluginEnabledEventArgs> OnPluginEnabled => _onPluginEnabled.Access;
    public IEventAccess<PluginDisabledEventArgs> OnPluginDisabled => _onPluginDisabled.Access;


    public PluginsManager(string pluginsDirectory, string librariesDirectory)
    {
        _pluginBase = typeof(TBase);
        _pluginUpdater = new PluginUpdater();
        _context = new AssemblyLoadContext(CONTEXT_NAME, true);
        _pluginsDirectory = new DllDirectory(ReferenceType.Plugin, pluginsDirectory, _context!, this);
        _librariesDirectory = new DllDirectory(ReferenceType.Library, librariesDirectory, _context!, this);
        Directories = [_pluginsDirectory, _librariesDirectory];
    }


    public void DisablePlugin(PluginBase plugin)
    {
        if (!plugin.IsEnabled)
            throw new PluginException("Plugin is already disabled.");
        _pluginUpdater.InvokePluginAction(plugin, PluginAction.Disable);
        _onPluginDisabled.Raise(new PluginDisabledEventArgs(plugin));
    }

    public void EnablePlugin(PluginBase plugin)
    {
        if (plugin.IsEnabled)
            throw new PluginException("Plugin is already enabled.");
        _pluginUpdater.InvokePluginAction(plugin, PluginAction.Enable);
        _onPluginEnabled.Raise(new PluginEnabledEventArgs(plugin));
    }

    public void UnloadPlugin(PluginBase pluginBase)
    {
    }

    public Reference LoadLibrary(string file) => _librariesDirectory.LoadReference(file, false).First();

    public IEnumerable<Reference> LoadLibraryWithReferences(string file) =>
        _librariesDirectory.LoadReference(file, true);

    public PluginLoadResult<TBase> LoadPlugin(Type type)
    {
        if (_pluginsTypes.ContainsKey(type))
            return new PluginLoadResult<TBase>(new PluginLoadException($"Plugin {type.Name} is already loaded."));
        if (type is not { IsAbstract: false, IsClass: true })
            return new PluginLoadResult<TBase>(new PluginLoadException(
                $"The {type.Name} type cannot be loaded as a plugin because it does not instantiable class."));

        if (type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(c => c.GetParameters().Length == 0) is not { } ctor)
            return new PluginLoadResult<TBase>(
                new PluginLoadException($"Plugin of type {type} has no constructors without parameters."));

        var plugin = (TBase)RuntimeHelpers.GetUninitializedObject(type);
        _pluginManagerField.SetValue(plugin, this);
        try
        {
            ctor.Invoke(plugin, null);
            _onPluginLoaded.Raise(new PluginLoadedEventArgs(plugin));
        }
        catch (Exception e)
        {
            if (e is not TargetInvocationException tie) throw;
            return new PluginLoadResult<TBase>(tie.InnerException ?? e);
        }
        _pluginsTypes.Add(type, plugin);
        _plugins.Add(plugin);
        return new PluginLoadResult<TBase>(plugin);
    }


    public PluginLoadResult<TBase> LoadPlugin(string file) => LoadPluginInternal(file, false).First();
    IPluginLoadResult IPluginsManager.LoadPlugin(string file) => LoadPlugin(file);

    public IEnumerable<PluginLoadResult<TBase>> LoadPluginWithReferences(string file) => LoadPluginInternal(file, true);

    IEnumerable<IPluginLoadResult> IPluginsManager.LoadPluginWithReferences(string file) =>
        LoadPluginInternal(file, true);

    private IEnumerable<PluginLoadResult<TBase>> LoadPluginInternal(string file, bool tryLoadReferences)
    {
        foreach (var reference in _pluginsDirectory.LoadReference(file, tryLoadReferences))
        {
            if (reference.Type != ReferenceType.Plugin) continue;
            Type? pluginType = null;
            var assembly = reference.Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (type is not { IsAbstract: false, IsClass: true } || !_pluginBase.IsAssignableFrom(type))
                    continue;
                if (pluginType is not null)
                    throw new PluginLoadException($"Assembly {assembly.FullName} has more than one plugin.");
                pluginType = type;
            }

            if (pluginType is null)
                throw new PluginLoadException($"Assembly {assembly.FullName} has no plugin type.");
            yield return LoadPlugin(pluginType);
        }
    }

    public void DisablePlugin(TBase plugin) => DisablePlugin((PluginBase)plugin);

    public void EnablePlugin(TBase plugin) => EnablePlugin((PluginBase)plugin);

    public void Reload(ReloadType reloadType)
    {
        _context?.Unload();
        _context = new AssemblyLoadContext(CONTEXT_NAME, true);
        foreach (var plugin in _plugins)
        {
            if (plugin.IsEnabled)
                DisablePlugin(plugin);
            if (plugin is IDisposable disposable) disposable.Dispose();
            else if (plugin is IAsyncDisposable asyncDisposable) asyncDisposable.DisposeAsync().AsTask().Wait();
        }

        _plugins = [];
        _pluginsTypes = [];
        foreach (var folder in Directories) folder.Reload(_context, reloadType);
    }

    /*
    PluginLoadResult<PluginBase> IPluginsSpace.LoadPlugin(Type type)
    {
        if (!_pluginBase.IsAssignableFrom(type))
            return new PluginLoadResult<PluginBase>(new PluginLoadException(
                $"The {type.Name} type cannot be loaded as a plugin because it does not inherit from type {_pluginBase.Name}."));
        return LoadPlugin(type);
    }
    */
}