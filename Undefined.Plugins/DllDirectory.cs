using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Undefined.Plugins.Exceptions;

namespace Undefined.Plugins;

public class DllDirectory
{
    private const string DLL_EXTENSION = ".dll";
    private readonly ReferenceType _folderType;
    private readonly string _dllsDirectory;
    private readonly string _tempDirectory;

    private List<Reference> _references = [];
    private AssemblyLoadContext _context;
    private Dictionary<string, Reference> _referencesNames = [];

    public IPluginsManager Manager { get; }

    public IReadOnlyList<Reference> References => _references.AsReadOnly();

    public DllDirectory(ReferenceType folderType, string dllsDirectory, AssemblyLoadContext context,
        IPluginsManager manager)
    {
        Manager = manager;
        _folderType = folderType;
        _dllsDirectory = dllsDirectory;
        _context = context;
        _tempDirectory = Path.Combine(dllsDirectory, ".temp");
        var info = new DirectoryInfo(_tempDirectory);
        if (!info.Exists) info.Create();
        info.Attributes |= FileAttributes.Hidden;
    }

    public IEnumerable<Reference> LoadReference(string file, bool tryLoadReferences)
    {
        if (Path.IsPathRooted(file))
            throw new ReferenceLoadException($"The file path must be relative to the '{_folderType}' folder.");
        file = Path.Combine(_dllsDirectory, file);
        if (!File.Exists(file))
            throw new PluginException($"File {file} does not exists.");
        var assemblyName = AssemblyName.GetAssemblyName(file);
        var references = LoadDllInternalReflection(file, tryLoadReferences).ToArray();
        if (references.Length == 0)
            throw new ReferenceLoadException(
                $"Reference with assembly name {assemblyName.Name} and version {assemblyName.Version} already loaded.");
        return references;
    }

    private IEnumerable<Reference> LoadDllInternalReflection(string fileOriginal, bool tryLoadReferences)
    {
        var loadedReference = LoadRefInternal(fileOriginal);
        if (tryLoadReferences)
        {
            var referencedAssemblies = loadedReference.Assembly.GetReferencedAssemblies();
            if (referencedAssemblies.Length == 0) yield break;
            foreach (var folder in Manager.Directories)
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

        yield return loadedReference;
    }

    private Reference LoadRefInternal(string fileOriginal)
    {
        var copyFile = CopyDllToTemp(fileOriginal);
        Assembly assembly;
        assembly = _context.LoadFromAssemblyPath(copyFile);
        using (_context.EnterContextualReflection()) Assembly.Load(assembly.GetName());
        var assemblyName = assembly.GetName();
        if (_referencesNames.ContainsKey(assemblyName.Name!))
            throw new PluginLoadException($"Reference with name {assemblyName.Name} already loaded.");
        var referenceInfo = new ReferenceInfo(assemblyName.Name!, fileOriginal, copyFile,
            assemblyName.Version!);
        var loadedReference = new Reference(referenceInfo, assembly, _folderType);
        _referencesNames.Add(referenceInfo.Name, loadedReference);
        _references.Add(loadedReference);
        return loadedReference;
    }

    private string CopyDllToTemp(string fileOriginal)
    {
        var copyFile = Path.Combine(_tempDirectory, Path.GetFileName(fileOriginal));
        if (File.Exists(copyFile)) File.Delete(copyFile);
        File.Copy(fileOriginal, copyFile);
        return copyFile;
    }

    public ReloadResult Reload(AssemblyLoadContext newContext, ReloadType reloadType)
    {
        _context = newContext;
        foreach (var file in Directory.EnumerateFiles(_tempDirectory)) File.Delete(file);
        var notLoaded = new List<ReferenceInfo>();
        if (reloadType == ReloadType.ReloadExisted)
        {
            var prevReferences = _references;
            _references = [];
            _referencesNames = [];
            foreach (var reference in prevReferences)
            {
                var info = reference.Info;
                if (!File.Exists(info.OriginalFile))
                {
                    notLoaded.Add(info);
                    continue;
                }

                LoadRefInternal(info.OriginalFile);
            }
        }
        else
        {
            _referencesNames = [];
            _references = [];
            foreach (var file in IterateFiles()) LoadRefInternal(file);
        }

        return new ReloadResult(reloadType, notLoaded);
    }

    private IEnumerable<string> IterateFiles()
    {
        foreach (var file in Directory.GetFiles(_dllsDirectory))
        {
            if (Path.GetExtension(file) == DLL_EXTENSION)
                yield return file;
        }
    }
}