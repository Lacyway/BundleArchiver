using System.Windows;
using BundleArchiver.ViewModels;

namespace BundleArchiver;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
#if RELEASE
        (DataContext as MainViewModel).RunCheck(); 
#endif
    }


}