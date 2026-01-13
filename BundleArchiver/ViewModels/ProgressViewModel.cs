using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BundleArchiver.Commands;

namespace BundleArchiver.ViewModels;

public class ProgressViewModel : INotifyPropertyChanged
{
    public ProgressViewModel()
    {
        CancelCommand = new RelayCommand(Cancel, () => CanCancel);
        ContinueCommand = new RelayCommand(() => CloseAction?.Invoke(), () => CanContinue);

        CancellationTokenSource = new();
    }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    private string _status = "Starting...";
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    private bool _canCancel;
    public bool CanCancel
    {
        get => _canCancel;
        set { _canCancel = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    private bool _canContinue;
    public bool CanContinue
    {
        get => _canContinue;
        set { _canContinue = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    public ICommand CancelCommand { get; }
    public ICommand ContinueCommand { get; }

    public Action? CloseAction { get; set; }

    public CancellationTokenSource CancellationTokenSource { get; }

    public void Cancel()
    {
        CancellationTokenSource?.Cancel();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
