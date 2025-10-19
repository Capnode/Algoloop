using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoloop.UI.Avalonia.ViewModels;

/// <summary>
/// Main ViewModel for the Avalonia application.
/// This is a minimal implementation to demonstrate the migration.
/// In a full migration, this would use the existing ViewModels from Algoloop.Wpf.ViewModels.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public MainViewModel()
    {
        // Initialize with demo data
        Title = "Algoloop - Avalonia UI Demo";
    }

    public string Title { get; }

    [RelayCommand]
    private void Save()
    {
        StatusMessage = "Saving...";
        // TODO: Implement save functionality
        StatusMessage = "Saved successfully";
    }

    [RelayCommand]
    private void Exit()
    {
        // TODO: Implement exit functionality
        System.Environment.Exit(0);
    }
}

