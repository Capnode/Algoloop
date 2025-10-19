using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoloop.Avalonia.ViewModels;

public class LogItem
{
    public string Time { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class LogViewModel : ObservableObject
{
    public ObservableCollection<LogItem> Logs { get; } = new();

    public LogViewModel()
    {
        // Add some sample log items
        Logs.Add(new LogItem { Time = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Level = "INFO", Message = "Application started" });
    }
}

public class MainViewModel : ObservableObject
{
    private bool _isBusy;
    private string _statusMessage = "Ready";

    public MainViewModel()
    {
        SaveCommand = new RelayCommand(SaveConfig, () => !IsBusy);
        ExitCommand = new RelayCommand<object>(DoExit);
        LogViewModel = new LogViewModel();
    }

    public static string Title => "Algoloop Avalonia (Preview)";

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public LogViewModel LogViewModel { get; }

    public RelayCommand SaveCommand { get; }
    public RelayCommand<object> ExitCommand { get; }

    private void SaveConfig()
    {
        StatusMessage = "Configuration saved";
    }

    private void DoExit(object? window)
    {
        if (window is global::Avalonia.Controls.Window w)
        {
            w.Close();
        }
    }
}
