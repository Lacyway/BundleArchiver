using System;
using Avalonia.Controls;
using BundleArchiverAvalonia.ViewModels;

namespace BundleArchiverAvalonia;

public partial class MessageBox : Window
{
    public MessageBox()
    {
        var viewModel = new MessageBoxViewModel();

        InitializeComponent();
        DataContext = viewModel;
    }

    public void SetText(string title, string message)
    {
        (DataContext as MessageBoxViewModel)!.WindowTitle = title;
        (DataContext as MessageBoxViewModel)!.Message = message;
    }

    public void SetCloseAction(Action closeAction)
    {
        (DataContext as MessageBoxViewModel)!.CloseAction = closeAction;
    }
}