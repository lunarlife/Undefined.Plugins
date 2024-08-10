namespace Undefined.Plugins;

public abstract class PluginBase
{
    public bool IsEnabled { get; }
    public IPluginsManager PluginsManager { get; }
    protected virtual void OnEnable()
    {
    }

    protected virtual void OnDisable()
    {
    }

    protected virtual void OnUnload()
    {
    }

    public void Unload() => PluginsManager.UnloadPlugin(this);
    public void Enable() => PluginsManager.EnablePlugin(this);
    public void Disable() => PluginsManager.DisablePlugin(this);
}