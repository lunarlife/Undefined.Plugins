using Undefined.Events;
using Undefined.Plugins.Events.Plugins;
using Undefined.Plugins.Libraries;

namespace Undefined.Plugins;

internal interface IPluginBase
{
    public PluginData Data { get; }
    public bool IsEnabled { get; }
    public bool IsLoaded { get; }
}

public abstract class PluginBase<T> : IPluginBase where T : PluginBase<T>
{
    private readonly Event<PluginDisabledEventArgs<T>> _onPluginDisabled = new();
    private readonly Event<PluginEnabledEventArgs<T>> _onPluginEnabled = new();
    private readonly Event<PluginUnloadEventArgs<T>> _onPluginUnload = new();

    public PluginsManager<T> PluginsManager { get; private set; }

    public IEventAccess<PluginUnloadEventArgs<T>> OnPluginUnload => _onPluginUnload.Access;
    public IEventAccess<PluginEnabledEventArgs<T>> OnPluginEnabled => _onPluginEnabled.Access;
    public IEventAccess<PluginDisabledEventArgs<T>> OnPluginDisabled => _onPluginDisabled.Access;
    public PluginData Data { get; private set; }
    public bool IsEnabled { get; private set; }
    public bool IsLoaded { get; private set; }

    public void ToggleEnable()
    {
        if (IsEnabled) Disable();
        else Enable();
    }
    
    internal void Init(ILibrary library, PluginsManager<T> manager, PluginAttribute attribute)
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
                _onPluginEnabled.Raise(new PluginEnabledEventArgs<T>(this));
                break;
            case PluginAction.Disable:
                IsEnabled = false;
                OnDisable();
                _onPluginDisabled.Raise(new PluginDisabledEventArgs<T>(this));
                break;
            case PluginAction.Unload:
                OnUnload();
                _onPluginUnload.Raise(new PluginUnloadEventArgs<T>(this));
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

    public PluginLoadResult<T> Reload() => PluginsManager.ReloadPlugin((T)this);
    public bool TryReload(out PluginLoadResult<T>? result) => PluginsManager.TryReloadPlugin((T)this, out result);

    public void Unload() => PluginsManager.UnloadPlugin((T)this);
    public bool TryUnload() => PluginsManager.TryUnloadPlugin((T)this);
    public void Enable() => PluginsManager.EnablePlugin((T)this);
    public void Disable() => PluginsManager.DisablePlugin((T)this);
    public override string ToString() => Data.ToString();
}