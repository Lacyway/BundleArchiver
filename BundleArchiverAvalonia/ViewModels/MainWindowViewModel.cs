using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using BundleArchiverAvalonia.Converters;
using BundleArchiverAvalonia.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BundleArchiverAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        _canScan = true;
        Mods = [];
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private bool _canScan;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private bool _canStart;

    [ObservableProperty]
    private bool _highCompression;

    public ObservableCollection<ModInfo> Mods { get; set; }

    [RelayCommand(CanExecute = nameof(CanScan))]
    public async Task Scan()
    {
        Debug.WriteLine("Scanning");
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
        /*if (!CanStart)
        {
            ShowDialog("No mods with bundles could be found.");
        }*/
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task Start()
    {
        var progressVM = new ProgressWindowViewModel();
        var progressWindow = new ProgressWindow
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            DataContext = progressVM
        };

        progressVM.CloseAction += progressWindow.Close;

        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            progressWindow.Show(desktop!.MainWindow!);
            desktop!.MainWindow!.IsEnabled = false;
        }
        else
        {
            throw new Exception("Could not find main window");
        }

        progressWindow.Closed += (s, e) =>
        {
            progressVM.CancellationTokenSource.Cancel();
            desktop!.MainWindow!.IsEnabled = true;
        };
        progressWindow.Closing += (s, e) => progressVM.CancellationTokenSource.Cancel();

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

                    var entry = zip.CreateEntry(entryName, HighCompression ? CompressionLevel.SmallestSize : CompressionLevel.Fastest);
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
                .OrderByDescending(f => f.Length);
            var collection = new ObservableCollection<FileInfo>(bundleFiles);
            var name = bundleDir.Parent?.Name ?? "Unknown mod";
            files.Add(new ModInfo(name, collection));
        }

        return [.. files];
    }

    [RelayCommand]
    public static void CloseDialog()
    {

    }
}
