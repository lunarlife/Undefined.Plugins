using System.Reflection;
using System.Runtime.Loader;
using Undefined.Events;
using Undefined.Plugins.Events.Libraries;
using Undefined.Plugins.Exceptions;

namespace Undefined.Plugins.Libraries;

public class LibrariesDirectory
{
    private const string DLL_EXTENSION = ".dll";

    private readonly List<RuntimeLibrary> _libraries = [];
    private readonly Dictionary<LibraryData, RuntimeLibrary> _librariesData = [];
    private readonly Event<LibraryLoadedEventArgs> _onLibraryLoaded = new();

    internal IPluginsManager PluginsManager { get; }

    public IReadOnlyList<RuntimeLibrary> Libraries => _libraries.AsReadOnly();
    public IEventAccess<LibraryLoadedEventArgs> OnLibraryLoaded => _onLibraryLoaded.Access;

    public DirectoryType Type { get; }

    public string TempDirectory { get; }

    public string DllsDirectory { get; }

    internal LibrariesDirectory(DirectoryType folderType, string dllsDirectory, IPluginsManager pluginsManager)
    {
        PluginsManager = pluginsManager;
        Type = folderType;
        DllsDirectory = dllsDirectory;
        TempDirectory = Path.Combine(dllsDirectory, ".temp");
        var info = new DirectoryInfo(TempDirectory);
        if (!info.Exists) info.Create();
        info.Attributes |= FileAttributes.Hidden;
    }

    public RuntimeLibrary LoadLibrary(string fileName) => LoadLibraryInternal(fileName, false).First();

    public IReadOnlyList<RuntimeLibrary> LoadLibraryWithReferences(string fileName) =>
        LoadLibraryInternal(fileName, true);

    internal IReadOnlyList<RuntimeLibrary> LoadLibraryInternal(string fileName, bool tryLoadReferences)
    {
        if (!fileName.EndsWith(DLL_EXTENSION)) fileName += DLL_EXTENSION;
        if (Path.IsPathRooted(fileName))
            throw new LibraryLoadException($"The file path must be relative to the '{Type}' folder.");
        var fullPath = Path.Combine(DllsDirectory, fileName);
        if (!File.Exists(fullPath))
            throw new PluginException($"File {fileName} does not exists.");
        var libraries = LoadDllInternalReflection(fullPath, tryLoadReferences)
            .ToArray();
        if (libraries.Length == 0)
            throw new LibraryLoadException(
                $"Library at path {fileName} already exists.");
        return libraries;
    }

    private IEnumerable<RuntimeLibrary> LoadDllInternalReflection(string fileOriginal, bool tryLoadReferences)
    {
        var loadedLibrary = LoadRefInternal(fileOriginal);
        if (tryLoadReferences)
        {
            var referencedAssemblies = loadedLibrary.Assembly.GetReferencedAssemblies();
            if (referencedAssemblies.Length == 0) yield break;
            foreach (var folder in PluginsManager.Directories)
            foreach (var file in folder.IterateFiles())
            {
                var fileAssemblyName = AssemblyName.GetAssemblyName(file).Name;
                foreach (var refAssemblyName in referencedAssemblies)
                {
                    if (fileAssemblyName != refAssemblyName.Name) continue;
                    foreach (var reference in
                             folder.LoadDllInternalReflection(refAssemblyName.Name!, tryLoadReferences))
                        yield return reference;
                }
            }
        }

        yield return loadedLibrary;
    }

    private RuntimeLibrary LoadRefInternal(string fileOriginal)
    {
        var preAssemblyName = AssemblyName.GetAssemblyName(fileOriginal);
        if (preAssemblyName.Name is not { } name || preAssemblyName.Version is not { } version)
            throw new LibraryLoadException("Assembly does not have name or version.");
        if (_librariesData.ContainsKey(new LibraryData(name, version)))
            throw new PluginLoadException($"Library with name {name} already loaded.");
        var referenceInfo =
            new LibraryInfo(name, fileOriginal, Path.Combine(TempDirectory, Path.GetFileName(fileOriginal)),
                version);
        var loadedLibrary = new RuntimeLibrary(this, referenceInfo);
        var assemblyName = loadedLibrary.Assembly.GetName();

        _librariesData.Add(new LibraryData(name, version), loadedLibrary);
        _libraries.Add(loadedLibrary);
        using (loadedLibrary.Context.EnterContextualReflection()) Assembly.Load(assemblyName);        
        foreach (var library in _libraries)
        {
            if (library == loadedLibrary) continue;
            using (loadedLibrary.Context.EnterContextualReflection())
            {
                library.Context.LoadFromAssemblyName(assemblyName);
            }

            using (library.Context.EnterContextualReflection())
            {
                loadedLibrary.Context.LoadFromAssemblyName(library.Assembly.GetName());
            }
        }

        _onLibraryLoaded.Raise(new LibraryLoadedEventArgs(loadedLibrary));
        return loadedLibrary;
    }


    public IEnumerable<string> IterateFiles()
    {
        foreach (var file in Directory.GetFiles(DllsDirectory))
            if (Path.GetExtension(file) == DLL_EXTENSION)
                yield return file;
    }

    internal void UnloadLibraryInternal(RuntimeLibrary library)
    {
        if (library.Directory != this) throw new LibraryUnloadException("Library has another directory.");
        _libraries.Remove(library);
        _librariesData.Remove(new LibraryData(library.Info.Name, library.Info.Version));
    }

    public bool TryGetLibrary(string assemblyName, Version assemblyVersion, out RuntimeLibrary? library) =>
        _librariesData.TryGetValue(new LibraryData(assemblyName, assemblyVersion), out library);

    private readonly struct LibraryData : IEquatable<LibraryData>
    {
        public string Name { get; }
        public Version Version { get; }

        public LibraryData(string name, Version version)
        {
            Name = name;
            Version = version;
        }

        public bool Equals(LibraryData other) => Name == other.Name && Version.Equals(other.Version);

        public override bool Equals(object? obj) => obj is LibraryData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Name, Version);
    }
}