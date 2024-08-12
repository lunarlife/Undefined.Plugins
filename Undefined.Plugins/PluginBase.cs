using Undefined.Events;
using Undefined.Plugins.Events.Plugins;
using Undefined.Plugins.Libraries;

namespace Undefined.Plugins;

public abstract class PluginBase
{
    private readonly Event<PluginDisabledEventArgs> _onPluginDisabled = new();
    private readonly Event<PluginEnabledEventArgs> _onPluginEnabled = new();
    private readonly Event<PluginUnloadEventArgs> _onPluginUnload = new();

    public bool IsEnabled { get; private set; }
    public IPluginsManager PluginsManager { get; private set; }
    public bool IsLoaded { get; private set; }
    public PluginData Data { get; private set; }

    public IEventAccess<PluginUnloadEventArgs> OnPluginUnload => _onPluginUnload.Access;
    public IEventAccess<PluginEnabledEventArgs> OnPluginEnabled => _onPluginEnabled.Access;
    public IEventAccess<PluginDisabledEventArgs> OnPluginDisabled => _onPluginDisabled.Access;

    internal void Init(ILibrary library, IPluginsManager manager, PluginAttribute attribute)
    {
        PluginsManager = manager;
        Data = new PluginData(library, attribute.Version, attribute.Name, attribute.IsUnloadable);
    }

    internal void DoActionInternal(PluginAction action)
    {
        switch (action)
        {
            case PluginAction.Load:
                OnLoad();
                IsLoaded = true;
                break;
            case PluginAction.Enable:
                IsEnabled = true;
                OnEnable();
                _onPluginEnabled.Raise(new PluginEnabledEventArgs(this));
                break;
            case PluginAction.Disable:
                IsEnabled = false;
                OnDisable();
                _onPluginDisabled.Raise(new PluginDisabledEventArgs(this));
                break;
            case PluginAction.Unload:
                OnUnload();
                _onPluginUnload.Raise(new PluginUnloadEventArgs(this));
                IsLoaded = false;
                break;
        }
    }

    protected virtual void OnLoad()
    {
    }

    protected virtual void OnEnable()
    {
    }

    protected virtual void OnDisable()
    {
    }

    protected virtual void OnUnload()
    {
    }

    public IPluginLoadResult Reload() => PluginsManager.ReloadPlugin(this);

    public void Unload() => PluginsManager.UnloadPlugin(this);
    public void Enable() => PluginsManager.EnablePlugin(this);
    public void Disable() => PluginsManager.DisablePlugin(this);
}