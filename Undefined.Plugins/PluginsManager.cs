using System.Reflection;
using System.Runtime.CompilerServices;
using Undefined.Events;
using Undefined.Plugins.Events.Plugins;
using Undefined.Plugins.Exceptions;
using Undefined.Plugins.Libraries;

namespace Undefined.Plugins;

public class PluginsManager<TBase> : IPluginsManager where TBase : PluginBase<TBase>
{
    private static readonly Type BaseType = typeof(TBase);
    private readonly LibrariesDirectory _librariesDirectory;

    private readonly Event<PluginLoadedEventArgs<TBase>> _onPluginLoaded = new();

    private readonly Type _pluginBase;
    private readonly List<TBase> _plugins = [];
    private readonly LibrariesDirectory _pluginsDirectory;
    private readonly Dictionary<ILibrary, TBase> _pluginsLibraries = [];
    private readonly Dictionary<string, TBase> _pluginsNames = [];
    private readonly Dictionary<Type, TBase> _pluginsTypes = [];

    private ILibrary? _mainPluginReference;

    public IEventAccess<PluginLoadedEventArgs<TBase>> OnPluginLoaded => _onPluginLoaded.Access;
    public TBase? MainPlugin { get; private set; }
    public IReadOnlyList<TBase> Plugins => new List<TBase>(_plugins);
    public IReadOnlyList<LibrariesDirectory> Directories { get; }


    private PluginsManager(string pluginsDirectory, string librariesDirectory)
    {
        _pluginBase = typeof(TBase);
        if (!Path.IsPathFullyQualified(pluginsDirectory))
            pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), pluginsDirectory);
        if (!Path.IsPathFullyQualified(librariesDirectory))
            librariesDirectory = Path.Combine(Directory.GetCurrentDirectory(), librariesDirectory);
        _pluginsDirectory = new LibrariesDirectory(DirectoryType.Plugin, pluginsDirectory, this);
        _librariesDirectory = new LibrariesDirectory(DirectoryType.Library, librariesDirectory, this);
        Directories = [_pluginsDirectory, _librariesDirectory];
    }

    public void Dispose()
    {
        foreach (var plugin in Plugins) plugin.Unload();
    }

    IPluginBase? IPluginsManager.GetPlugin(ILibrary library)
    {
        TryGetPlugin(library, out var plugin);
        return plugin;
    }


    public void ToggleEnablePlugin(TBase plugin) => plugin.ToggleEnable();

    public void DisablePlugin(TBase plugin)
    {
        if (!plugin.IsEnabled)
            throw new PluginException("Plugin is already disabled.");
        plugin.DoActionInternal(PluginAction.Disable);
    }

    public void EnablePlugin(TBase plugin)
    {
        if (plugin.IsEnabled)
            throw new PluginException("Plugin is already enabled.");
        plugin.DoActionInternal(PluginAction.Enable);
    }

    public bool TryUnloadPlugin(TBase plugin)
    {
        if (!HasPlugin(plugin)) return false;
        if (!plugin.Data.IsUnloadable) return false;
        UnloadPluginInternal(plugin);
        (plugin.Data.Library as RuntimeLibrary)?.Unload();
        return true;
    }

    public void UnloadPlugin(TBase plugin)
    {
        if (!TryUnloadPlugin(plugin)) throw new PluginUnloadException("Plugin is not unloadable.");
    }

    private void UnloadPluginInternal(TBase plugin)
    {
        if (plugin.IsEnabled)
            DisablePlugin(plugin);

        plugin.DoActionInternal(PluginAction.Unload);
        if (plugin is IDisposable disposable) disposable.Dispose();
        else if (plugin is IAsyncDisposable asyncDisposable) asyncDisposable.DisposeAsync().AsTask().Wait();
        _plugins.Remove(plugin);
        _pluginsTypes.Remove(plugin.GetType());
        _pluginsNames.Remove(plugin.Data.Name);
        _pluginsLibraries.Remove(plugin.Data.Library);
    }

    public RuntimeLibrary LoadLibrary(string fileName) =>
        _librariesDirectory.LoadLibrary(fileName);

    public IReadOnlyList<RuntimeLibrary> LoadLibraryWithReferences(string fileName) =>
        _librariesDirectory.LoadLibraryWithReferences(fileName);

    private PluginLoadResult<TBase> LoadMain<TMain>(bool enable) where TMain : TBase, new()
    {
        var type = typeof(TMain);
        var assembly = type.Assembly;
        var assemblyName = assembly.GetName();
        var info = new LibraryInfo(assemblyName.Name!, assembly.Location, assembly.Location, assemblyName.Version!);
        _mainPluginReference = new StaticLibrary(info, assembly);
        var result = CreateInstance(type, _mainPluginReference, enable);
        if (result.Status == PluginLoadStatus.Error)
            return result;
        MainPlugin = result.Plugin;
        return result;
    }

    private PluginLoadResult<TBase> CreateInstance(Type type, ILibrary library, bool enable)
    {
        if (type.GetCustomAttributes().FirstOrDefault(att => att is PluginAttribute) is not PluginAttribute attribute)
            return new PluginLoadResult<TBase>(
                new PluginLoadException($"Plugin type {type.Name} has no {nameof(PluginAttribute)}."));

        if (_pluginsTypes.ContainsKey(type))
            return new PluginLoadResult<TBase>(new PluginLoadException($"Plugin type {type.Name} is already loaded."));
        if (type is not { IsAbstract: false, IsClass: true })
            return new PluginLoadResult<TBase>(new PluginLoadException(
                $"The {type.Name} type cannot be loaded as a plugin because it does not instantiable class."));

        if (type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(c => c.GetParameters().Length == 0) is not { } ctor)
            return new PluginLoadResult<TBase>(
                new PluginLoadException($"Plugin type {type.Name} has no constructors without parameters."));

        var plugin = (TBase)RuntimeHelpers.GetUninitializedObject(type);
        plugin.Init(library, this, attribute);
        try
        {
            ctor.Invoke(plugin, null);
            _onPluginLoaded.Raise(new PluginLoadedEventArgs<TBase>(plugin));
            _pluginsTypes.Add(type, plugin);
            _pluginsNames.Add(plugin.Data.Name, plugin);
            _pluginsLibraries.Add(library, plugin);
            _plugins.Add(plugin);
            if (enable) plugin.Enable();
        }
        catch (Exception e)
        {
            if (e is not TargetInvocationException tie) throw;
            return new PluginLoadResult<TBase>(tie.InnerException ?? e);
        }

        return new PluginLoadResult<TBase>(plugin);
    }


    public PluginLoadResult<TBase> LoadPlugin(string file, bool enable = true, bool isUnloadable = true) =>
        LoadPluginInternal(file, false, enable).First();

    public IReadOnlyList<PluginLoadResult<TBase>> LoadPluginWithReferences(string file, bool enable = true) =>
        LoadPluginInternal(file, true, enable);

    private IReadOnlyList<PluginLoadResult<TBase>> LoadPluginInternal(string file, bool tryLoadReferences, bool enable)
    {
        var list = new List<PluginLoadResult<TBase>>();
        foreach (var reference in _pluginsDirectory.LoadLibraryInternal(file, tryLoadReferences))
        {
            if (reference.Directory.Type != DirectoryType.Plugin) continue;
            Type? pluginType = null;
            var assembly = reference.Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (type is not { IsAbstract: false, IsClass: true } || !_pluginBase.IsAssignableFrom(type))
                    continue;
                if (!type.GetCustomAttributes().Any(attribute => attribute is PluginAttribute)) continue;
                if (pluginType is not null)
                    throw new PluginLoadException($"Assembly {assembly.FullName} has more than one plugin.");
                pluginType = type;
            }

            if (pluginType is null)
                throw new PluginLoadException($"Assembly {assembly.FullName} has no plugin type.");
            list.Add(CreateInstance(pluginType, reference, enable));
        }

        return list;
    }

    public IReadOnlyList<PluginLoadResult<TBase>> Reload(ReloadType reloadType)
    {
        var list = new List<PluginLoadResult<TBase>>();
        if ((reloadType & ReloadType.ReloadExisted) != 0)
            foreach (var plugin in _plugins)
                list.Add(ReloadPlugin(plugin));

        if ((reloadType & ReloadType.LoadNew) != 0)
            foreach (var file in _pluginsDirectory.IterateFiles())
            {
                if (_plugins.Any(plugin => plugin.Data.Library.Info.OriginalFile == file)) continue;
                list.Add(LoadPlugin(Path.GetFileName(file)));
            }

        return list;
    }

    public PluginLoadResult<TBase> ReloadPlugin(TBase plugin)
    {
        if (!TryReloadPlugin(plugin, out var result))
            throw new PluginReloadException($"Not possible to reload plugin with {nameof(StaticLibrary)}.");
        return result!;
    }

    public bool TryReloadPlugin(TBase plugin, out PluginLoadResult<TBase>? result)
    {
        if (!plugin.Data.IsPossibleReload)
        {
            result = null;
            return false;
        }

        var library = (RuntimeLibrary)plugin.Data.Library;
        var type = plugin.GetType();
        UnloadPluginInternal(plugin);
        library.UnloadDllInternal();
        library.LoadDllInternal();
        result = CreateInstance(type, library, true);
        return true;
    }

    public IReadOnlyList<T> GetPlugins<T>() where T : TBase => (IReadOnlyList<T>)GetPlugins(typeof(T));

    public IReadOnlyList<TBase> GetPlugins(Type type)
    {
        if (!BaseType.IsAssignableFrom(type))
            throw new PluginException(
                $"In the current {nameof(PluginsManager<TBase>)} all plugins must inherit from {nameof(TBase)}.");
        if (type == BaseType)
            return GetPlugin(type) is { } plugin ? [plugin] : [];
        var plugins = new List<TBase>();
        foreach (var plugin in _plugins)
        {
            if (!type.IsInstanceOfType(plugin))
                continue;
            plugins.Add(plugin);
        }

        return plugins;
    }

    public T? GetPlugin<T>() where T : TBase, new() => GetPlugin(typeof(T)) as T;

    public TBase? GetPlugin(Type type)
    {
        if (!BaseType.IsAssignableFrom(type))
            throw new PluginException(
                $"In the current {nameof(PluginsManager<TBase>)} all plugins must inherit from {nameof(TBase)}.");
        if (type is not { IsAbstract: false, IsClass: true })
            throw new PluginException("Type must be instantiable class.");
        TryGetPlugin(type, out var plugin);
        return plugin;
    }

    public bool TryGetPlugin(ILibrary library, out TBase? plugin)
    {
        if (_pluginsLibraries.TryGetValue(library, out var pl))
        {
            plugin = pl;
            return true;
        }

        plugin = null;
        return false;
    }

    public bool TryGetPlugin<T>(out T? plugin) where T : TBase, new()
    {
        var type = typeof(T);
        if (_pluginsTypes.TryGetValue(type, out var pl))
        {
            plugin = pl as T;
            return true;
        }

        plugin = null;
        return false;
    }

    public bool TryGetPlugin(Type type, out TBase? plugin)
    {
        if (_pluginsTypes.TryGetValue(type, out var pl))
        {
            plugin = pl;
            return true;
        }

        plugin = null;
        return false;
    }

    public bool TryGetPlugin(string pluginName, out TBase? plugin)
    {
        if (_pluginsNames.TryGetValue(pluginName, out var pl))
        {
            plugin = pl;
            return true;
        }

        plugin = null;
        return false;
    }

    public bool HasPlugin(Type type) => GetPlugin(type) != null;
    public bool HasPlugin(TBase plugin) => _plugins.Contains(plugin);
    public bool HasPlugin<T>() where T : TBase, new() => TryGetPlugin<T>(out _);

    public static PluginsManager<TBase> Create(string pluginsDirectory, string librariesDirectory) =>
        new(pluginsDirectory, librariesDirectory);

    public static PluginsManager<TBase> Create<TMain>(string pluginsDirectory, string librariesDirectory,
        out PluginLoadResult<TBase> mainLoadResult,
        bool enableMain = true) where TMain : TBase, new()
    {
        var manager = new PluginsManager<TBase>(pluginsDirectory, librariesDirectory);
        mainLoadResult = manager.LoadMain<TMain>(enableMain);
        return manager;
    }

    public static PluginsManager<TBase> Create<TMain>(out PluginLoadResult<TBase> mainLoadResult,
        bool enableMain = true) where TMain : TBase, new() =>
        Create<TMain>("plugins", "libraries", out mainLoadResult, enableMain);
}