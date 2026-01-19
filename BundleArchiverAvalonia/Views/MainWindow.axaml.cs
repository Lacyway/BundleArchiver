using Avalonia.Controls;
using BundleArchiverAvalonia.ViewModels;

namespace BundleArchiverAvalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += MainWindow_Opened;
    }

    private async void MainWindow_Opened(object? sender, System.EventArgs e)
    {
        await (DataContext as MainWindowViewModel)!.RunCheck();
    }
}