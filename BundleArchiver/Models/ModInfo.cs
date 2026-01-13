using System.IO;

namespace BundleArchiver.Models;

public record ModInfo(string Name, IReadOnlyList<FileInfo> BundleFiles)
{
    public long TotalSize => BundleFiles.Sum(f => f.Length);
}
