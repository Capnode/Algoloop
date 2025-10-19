using Algoloop.Wpf.Model;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Algoloop.Wpf.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private bool _isBusy;
        private string _statusMessage = string.Empty;

        public MainViewModel(
            SettingsViewModel settingsViewModel,
            MarketsViewModel marketsViewModel,
            StrategiesViewModel strategiesViewModel,
            ResearchViewModel researchViewModel,
            LogViewModel logViewModel)
        {
            SettingsViewModel = settingsViewModel;
            MarketsViewModel = marketsViewModel;
            StrategiesViewModel = strategiesViewModel;
            ResearchViewModel = researchViewModel;
            LogViewModel = logViewModel;

            SaveCommand = new RelayCommand(() => SaveConfig(), () => !IsBusy);
            ExitCommand = new RelayCommand<object?>(_ => DoExit(), _ => !IsBusy);
        }

        public ICommand SaveCommand { get; }
        public ICommand ExitCommand { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public MarketsViewModel MarketsViewModel { get; }
        public StrategiesViewModel StrategiesViewModel { get; }
        public ResearchViewModel ResearchViewModel { get; }
        public LogViewModel LogViewModel { get; }

        public static string Title => $"{AboutModel.Title} {AboutModel.Version}";

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

        public void SaveConfig()
        {
            StatusMessage = "Configuration saved";
        }

        public void DoSettings(bool update)
        {
            StatusMessage = "Settings updated";
        }

        private void DoExit()
        {
            System.Environment.Exit(0);
        }
    }

    public class SettingsViewModel : ViewModelBase { }

    public class MarketsViewModel : ViewModelBase
    {
        public ObservableCollection<object> Markets { get; } = new ObservableCollection<object>();
    }

    public class StrategiesViewModel : ViewModelBase
    {
        public ObservableCollection<object> Strategies { get; } = new ObservableCollection<object>();
    }

    public class ResearchViewModel : ViewModelBase
    {
        public string Source => "about:blank";

        public void StopJupyter()
        {
            // Stub implementation
        }
    }

    public class LogViewModel : ViewModelBase
    {
        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();
    }
}
