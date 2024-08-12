namespace Undefined.Plugins;

public interface IPluginLoadResult
{
    public PluginBase? Plugin { get; }
    public PluginLoadStatus Status { get; }
    public Exception? Exception { get; }
}

public class PluginLoadResult<T> : IPluginLoadResult where T : PluginBase
{
    public T? Plugin { get; }
    public Exception? Exception { get; }
    public PluginLoadStatus Status { get; }

    PluginBase? IPluginLoadResult.Plugin => Plugin;

    public PluginLoadResult(T plugin)
    {
        Plugin = plugin;
        Status = PluginLoadStatus.Success;
    }

    public PluginLoadResult(Exception exception)
    {
        Exception = exception;
        Status = PluginLoadStatus.Error;
    }
}