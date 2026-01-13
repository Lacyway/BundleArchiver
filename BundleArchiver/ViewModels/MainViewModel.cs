using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BundleArchiver.Commands;
using BundleArchiver.Converters;
using BundleArchiver.Models;
using BundleArchiver.Windows;

namespace BundleArchiver.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public MainViewModel()
    {
        Mods = [];
        ScanCommand = new RelayCommand(async () => await ScanAsync(), () => CanScan);
        StartCommand = new RelayCommand(async () => await StartAsync(), () => CanStart);
        CloseDialogCommand = new RelayCommand(CloseDialog, () => IsDialogOpen);

#if DEBUG
        CanScan = true;
#endif
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<ModInfo> Mods { get; set; }

    private bool _canScan;
    public bool CanScan
    {
        get => _canScan;
        set
        {
            _canScan = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool _canStart;
    public bool CanStart
    {
        get => _canStart;
        set
        {
            _canStart = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool _isDialogOpen;
    public bool IsDialogOpen
    {
        get => _isDialogOpen;
        set { _isDialogOpen = value; OnPropertyChanged(); }
    }

    private string? _dialogText;
    public string? DialogText
    {
        get => _dialogText;
        set { _dialogText = value; OnPropertyChanged(); }
    }

    private bool _highCompression;
    public bool HighCompression
    {
        get => _highCompression;
        set { _highCompression = value; OnPropertyChanged(); }
    }


    public ICommand ScanCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand CloseDialogCommand { get; }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void ShowDialog(string message)
    {
        DialogText = message;
        IsDialogOpen = true;
    }

    private void CloseDialog()
    {
        IsDialogOpen = false;
    }

    private async Task ScanAsync()
    {
        CanScan = false;
        Mods.Clear();

        var dir = Path.Combine(AppContext.BaseDirectory, "SPT/user/mods");
        var mods = await GetAllBundleFiles(dir);
        foreach (var mod in mods)
        {
            Mods.Add(mod);
        }

        CanScan = true;

        CanStart = Mods.Count > 0;
        if (!CanStart)
        {
            ShowDialog("No mods with bundles could be found.");
        }
    }

    private static async Task<ModInfo[]> GetAllBundleFiles(string dir)
    {
        var rootDir = new DirectoryInfo(dir);
        var subDirs = rootDir.GetDirectories("*",
            SearchOption.TopDirectoryOnly);

        var bundleDirs = new List<DirectoryInfo>();

        foreach (var subDir in subDirs)
        {
            var bundleDir = subDir.GetDirectories("*", SearchOption.TopDirectoryOnly)
                      .FirstOrDefault(x => x.Name.Equals("bundles", StringComparison.OrdinalIgnoreCase));

            if (bundleDir != null)
            {
                bundleDirs.Add(bundleDir);
            }
        }

        var files = new List<ModInfo>();

        foreach (var bundleDir in bundleDirs)
        {
            var bundleFiles = bundleDir.EnumerateFiles("*.bundle", SearchOption.AllDirectories)
                .OrderByDescending(f => f.Length)
                .ToImmutableArray();
            var name = bundleDir.Parent?.Name ?? "Unknown mod";
            files.Add(new ModInfo(name, bundleFiles));
        }

        return [.. files];
    }

    private async Task StartAsync()
    {
        var mainWindow = App.Current.MainWindow;
        var progressVM = new ProgressViewModel();
        var progressWindow = new ProgressWindow
        {
            DataContext = progressVM,
            Owner = mainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        mainWindow.IsEnabled = false;

        progressWindow.Closed += (s, e) => mainWindow.IsEnabled = true;
        progressWindow.Closing += (sender, e) => progressVM?.Cancel();

        progressVM.CloseAction += progressWindow.Close;

        progressWindow.Show();

        IProgress<(double percent, string status)> progress = new Progress<(double percent, string status)>(p =>
        {
            if (p.percent != -1)
            {
                progressVM.Progress = p.percent;
            }
            progressVM.Status = p.status;
        });

        progressVM.CanCancel = true;

        try
        {
            await ZipFiles([.. Mods], progress, progressVM.CancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            progress?.Report((-1, "Zipping canceled by user."));
        }
        catch (Exception ex)
        {
            progress?.Report((-1, $"Error: {ex.Message}"));
        }

        progressVM.CanCancel = false;
        progressVM.CanContinue = true;
    }

    private async Task ZipFiles(List<ModInfo> modInfos, IProgress<(double percent, string status)> progress,
        CancellationToken cancellationToken = default)
    {
        var zipPath = Path.Combine(AppContext.BaseDirectory, "ArchivedBundles.zip");

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        await using (var zip = await ZipFile.OpenAsync(zipPath, ZipArchiveMode.Create, cancellationToken))
        {
            foreach (var modInfo in modInfos)
            {
                var total = modInfo.BundleFiles.Count;
                for (var i = 0; i < total; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var file = modInfo.BundleFiles[i];

                    var index = file.FullName.IndexOf("bundles", StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                    {
                        throw new InvalidOperationException($"File path does not contain a 'bundles' folder: {file.FullName}");
                    }

                    var relativePath = file.FullName[(index + "bundles".Length)..]
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    var entryName = Path.Combine("SPT/user/cache/bundles", relativePath);

                    var entry = zip.CreateEntry(entryName, _highCompression ? CompressionLevel.SmallestSize : CompressionLevel.Fastest);
                    await using var entryStream = entry.Open();
                    await using var fileStream = File.OpenRead(file.FullName);

                    await fileStream.CopyToAsync(entryStream, cancellationToken);

                    progress?.Report(((i + 1) * 100.0 / total, $"Zipping {file.Name} ({i + 1}/{total})"));
                }
            }
        }

        var resultFile = new FileInfo(zipPath);

        progress?.Report((100, $"Completed! Size: {FileSizeConverter.FileLengthToFileSize(resultFile.Length)}"));
    }

#if RELEASE
    public void RunCheck()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "SPT/user/mods");
        if (!Directory.Exists(path))
        {
            ShowDialog("Could not find the SPT folder.\nMake sure that you are running the tool from your SPT installation folder/server folder!");
            CanScan = false;
        }
        else
        {
            CanScan = true;
        }
    }
#endif
}
