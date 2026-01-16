using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BundleArchiverAvalonia.ViewModels;

public partial class ProgressWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string? _status;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _canCancel;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private bool _canContinue;

    public CancellationTokenSource CancellationTokenSource { get; set; } = new();

    public Action? CloseAction { get; set; }

    [RelayCommand(CanExecute = nameof(CanContinue))]
    public void Continue()
    {
        CloseAction?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    public void Cancel()
    {
        CancellationTokenSource.Cancel();
    }
}
