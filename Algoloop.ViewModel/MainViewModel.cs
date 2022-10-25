/*
 * Copyright 2018 Capnode AB
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Algoloop.Model;
using Algoloop.ViewModel.Properties;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using QuantConnect;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Algoloop.ViewModel.Internal;

namespace Algoloop.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private bool _isBusy;
        private string _statusMessage;
        private Task _initializer;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
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
            ExitCommand = new RelayCommand<Window>(
                window => DoExit(window), _ => !IsBusy);
            UpdateCommand = new RelayCommand(
                async () => await DoUpdate().ConfigureAwait(false), () => !IsBusy);
            WeakReferenceMessenger.Default.Register<MainViewModel, NotificationMessage, int>(
                this, 0, static (r, m) => r.OnStatusMessage(m));

            // Set working directory
            string appData = MainService.GetAppDataFolder();
            Directory.SetCurrentDirectory(appData);

            // Async initialize without blocking UI
            _initializer = Initialize();

            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public RelayCommand SaveCommand { get; }
        public RelayCommand<Window> ExitCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public MarketsViewModel MarketsViewModel { get; }
        public StrategiesViewModel StrategiesViewModel { get; }
        public ResearchViewModel ResearchViewModel { get; }
        public LogViewModel LogViewModel { get; }

        public static string Title => AboutModel.Title;

        /// <summary>
        /// Mark ongoing operation
        /// </summary>
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
            try
            {
                IsBusy = true;
                Messenger.Send(new NotificationMessage(Resources.SavingConfiguration), 0);

                // Save config files
                string programData = MainService.GetProgramDataFolder();
                string settings = Path.Combine(programData, "Settings.json");
                string markets = Path.Combine(programData, "Markets.json");
                string strategies = Path.Combine(programData, "Strategies.json");
                string logfile = Path.Combine(programData, "Algoloop.log");
                SettingsViewModel.Save(settings);
                MarketsViewModel.Save(markets);
                StrategiesViewModel.Save(strategies);

                // Create backup files
                string appData = MainService.GetAppDataFolder();
                File.Copy(settings, Path.Combine(appData, "Settings.json"), true);
                File.Copy(markets, Path.Combine(appData, "Markets.json"), true);
                File.Copy(strategies, Path.Combine(appData, "Strategies.json"), true);
                File.Copy(logfile, Path.Combine(appData, "Algoloop.log"), true);
            }
            finally
            {
                Messenger.Send(new NotificationMessage(string.Empty), 0);
                IsBusy = false;
            }
        }

        public async Task DoSettings(bool update)
        {
            Debug.Assert(IsUiThread(), "Not UI thread!");

            // Wait for initialization complete
            await _initializer.ConfigureAwait(true);
            string programData = MainService.GetProgramDataFolder();

            if (update)
            {
                // Save model and initialize
                SettingsViewModel.Save(Path.Combine(programData, "Settings.json"));

                // Async initialize without blocking UI
                _initializer = Initialize();
            }
            else
            {
                // Reload old settings
                await SettingsViewModel.ReadAsync(Path.Combine(programData, "Settings.json")).ConfigureAwait(true);
            }
        }

        private void OnStatusMessage(NotificationMessage message)
        {
            StatusMessage = message.Value;
            if (string.IsNullOrWhiteSpace(StatusMessage))
                return;

            Log.Trace(StatusMessage);
        }

        private void DoExit(Window window)
        {
            Debug.Assert(window != null);
            try
            {
                IsBusy = true;
                window.Close();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task Initialize()
        {
            try
            {
                IsBusy = true;
                Messenger.Send(new NotificationMessage(Resources.LoadingConfiguration), 0);

                // Initialize data folders
                string programFolder = MainService.GetProgramFolder();
                string appDataFolder = MainService.GetAppDataFolder();
                string programDataFolder = MainService.GetProgramDataFolder();
                string userDataFolder = MainService.GetUserDataFolder();
                Log.Trace($"Program folder: {programFolder}");
                Log.Trace($"AppData folder: {appDataFolder}");
                Log.Trace($"ProgramData folder: {programDataFolder}");
                Log.Trace($"UserData folder: {userDataFolder}");
                Directory.CreateDirectory(appDataFolder);
                Directory.CreateDirectory(programDataFolder);

                // Migrate existing files to new location
                MainService.DeleteFolders(appDataFolder, "temp*");
                MainService.DeleteFiles(appDataFolder, "*.log");
                MainService.CopyDirectory(appDataFolder, programDataFolder, false);
                MainService.DeleteFolders(appDataFolder, "*");

                // Update Market data
                MainService.CopyDirectory(
                    Path.Combine(programFolder, "Content/ProgramData"),
                    programDataFolder,
                    false);

                // Update User data
                MainService.CopyDirectory(
                    Path.Combine(programFolder, "Content/UserData"),
                    userDataFolder,
                    false);

                // Read settings
                await SettingsViewModel.ReadAsync(Path.Combine(programDataFolder, "Settings.json"))
                    .ConfigureAwait(true);

                // Set max backtests
                BacktestManager.SetSlots(SettingsViewModel.Model.MaxBacktests);

                // Set config
                Config.Set("data-directory", SettingsViewModel.Model.DataFolder);
                Config.Set("data-folder", SettingsViewModel.Model.DataFolder);
                Config.Set("cache-location", SettingsViewModel.Model.DataFolder);
                Config.Set("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");
                Globals.Reset();

                // Read configuration
                await MarketsViewModel.ReadAsync(Path.Combine(programDataFolder, "Markets.json"));
                await StrategiesViewModel.ReadAsync(Path.Combine(programDataFolder, "Strategies.json"))
                    .ConfigureAwait(true);

                // Update Data folder
                MainService.CopyDirectory(
                    Path.Combine(programFolder, "Content/ProgramData/market-hours"),
                    Path.Combine(Globals.DataFolder, "market-hours"),
                    true);
                MainService.CopyDirectory(
                    Path.Combine(programFolder, "Content/ProgramData/symbol-properties"),
                    Path.Combine(Globals.DataFolder, "symbol-properties"),
                    true);

                // Initialize Research page
                ResearchViewModel.Initialize();

                // Completed
                Messenger.Send(new NotificationMessage(
                    Resources.LoadingConfigurationCompleted), 0);
            }
            catch (Exception ex)
            {
                string message = $"{ex.Message} ({ex.GetType()})";
                Messenger.Send(new NotificationMessage(message), 0);
                Log.Error(ex, message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DoUpdate()
        {
            await _initializer.ConfigureAwait(false);
            try
            {
                IsBusy = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
