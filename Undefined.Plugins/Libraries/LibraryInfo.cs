namespace Undefined.Plugins.Libraries;

public class LibraryInfo : IEquatable<LibraryInfo>
{
    public string Name { get; }
    public string OriginalFile { get; }
    public string TempFile { get; }
    public Version Version { get; }

    public LibraryInfo(string name, string originalFile, string tempFile, Version version)
    {
        Name = name;
        OriginalFile = originalFile;
        TempFile = tempFile;
        Version = version;
    }

    public bool Equals(LibraryInfo? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name && Version.Equals(other.Version);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((LibraryInfo)obj);
    }

    public override int GetHashCode() => HashCode.Combine(Name, Version);
}