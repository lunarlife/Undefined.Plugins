using Undefined.Plugins.Libraries;

namespace Undefined.Plugins;

internal interface IPluginsManager : IDisposable
{
    public IReadOnlyList<LibrariesDirectory> Directories { get; }
    public IPluginBase? GetPlugin(ILibrary library);
}