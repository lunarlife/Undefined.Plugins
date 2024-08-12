using System.Reflection;
using System.Runtime.Loader;
using Undefined.Events;
using Undefined.Plugins.Events.Libraries;
using Undefined.Plugins.Exceptions;

namespace Undefined.Plugins.Libraries;

public interface ILibrary
{
    public LibraryInfo Info { get; }
    public Assembly Assembly { get; }
    public bool IsLoaded { get; }
}

public class StaticLibrary : ILibrary
{
    public LibraryInfo Info { get; }
    public Assembly Assembly { get; }
    public bool IsLoaded => true;

    public StaticLibrary(LibraryInfo info, Assembly assembly)
    {
        Info = info;
        Assembly = assembly;
    }
}

public class RuntimeLibrary : ILibrary
{
    private readonly Event<LibraryUnloadEventArgs> _onUnload = new();
    private FileStream _stream;

    public LibrariesDirectory Directory { get; }
    public AssemblyLoadContext Context { get; private set; }

    public IEventAccess<LibraryUnloadEventArgs> OnUnload => _onUnload.Access;
    public LibraryInfo Info { get; }
    public Assembly Assembly { get; private set; }
    public bool IsLoaded { get; private set; }

    public RuntimeLibrary(LibrariesDirectory directory, LibraryInfo info)
    {
        Directory = directory;
        Info = info;
        LoadDllInternal();
    }

    internal void LoadDllInternal()
    {
        CopyDllToTemp();
        Context = new UndefinedAssemblyLoadContext(this, $"{Directory.Type}_{Info.Name}_{Info.Version}");
        _stream = File.OpenRead(Info.TempFile);
        Assembly = Context.LoadFromStream(_stream);
        UpdateReflectionInternal();
        IsLoaded = true;
    }

    internal void UpdateReflectionInternal()
    {
        using (Context.EnterContextualReflection())
        {
            Assembly.Load(Assembly.GetName());
        }
    }

    public void Unload()
    {
        if (Directory.PluginsManager.TryGetPlugin(this, out var plugin) && !plugin!.Data.IsUnloadable)
            throw new LibraryUnloadException("Reference is not unloadable.");
        UnloadDllInternal();
        Directory.UnloadLibraryInternal(this);
    }

    internal void UnloadDllInternal()
    {
        _onUnload.Raise(new LibraryUnloadEventArgs(this));
        Context.Unload();
        _stream.Close();
        _stream.Dispose();
        IsLoaded = false;
    }

    private void CopyDllToTemp()
    {
        if (File.Exists(Info.TempFile)) File.Delete(Info.TempFile);
        File.Copy(Info.OriginalFile, Info.TempFile);
    }
}