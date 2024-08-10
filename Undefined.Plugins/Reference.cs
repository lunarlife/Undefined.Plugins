using System.Reflection;

namespace Undefined.Plugins;

public class Reference
{
    public ReferenceInfo Info { get; }
    public Assembly Assembly { get; }
    public ReferenceType Type { get; }
    
    public Reference(ReferenceInfo info, Assembly assembly, ReferenceType type)
    {
        Info = info;
        Assembly = assembly;
        Type = type;
    }
}