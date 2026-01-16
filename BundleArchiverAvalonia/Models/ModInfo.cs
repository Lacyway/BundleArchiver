using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace BundleArchiverAvalonia.Models;

public record ModInfo(string Name, ObservableCollection<FileInfo> BundleFiles)
{
    public long TotalSize => BundleFiles.Sum(f => f.Length);
}
