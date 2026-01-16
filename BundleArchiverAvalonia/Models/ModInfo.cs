using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BundleArchiverAvalonia.Models;

public record ModInfo(string Name, IReadOnlyList<FileInfo> BundleFiles)
{
    public long TotalSize => BundleFiles.Sum(f => f.Length);
}
