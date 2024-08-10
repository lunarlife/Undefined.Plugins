namespace Undefined.Plugins;

public class ReloadResult
{
    public ReloadType ReloadType { get; }
    public IReadOnlyList<ReferenceInfo> ReferencesNotLoaded { get; }

    public ReloadResult(ReloadType reloadType,  IReadOnlyList<ReferenceInfo> referencesNotLoaded)
    {
        ReloadType = reloadType;
        ReferencesNotLoaded = referencesNotLoaded;
    }
}