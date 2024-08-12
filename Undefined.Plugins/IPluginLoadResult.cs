namespace Undefined.Plugins;

public class PluginLoadResult<T> where T : PluginBase<T>
{
    public T? Plugin { get; }
    public Exception? Exception { get; }
    public PluginLoadStatus Status { get; }

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