using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BundleArchiverAvalonia.ViewModels;

public partial class MessageBoxViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private string? _windowTitle;

    public Action? CloseAction { get; set; }

    [RelayCommand]
    public void Close()
    {
        CloseAction?.Invoke();
    }
}
