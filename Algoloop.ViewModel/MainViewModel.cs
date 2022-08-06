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
                window => DoExit(window), window => !IsBusy);
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
        public RelayCommand SettingsCommand { get; }
        public RelayCommand<Window> ExitCommand { get; }
        public RelayCommand UpdateCommand { get; }

        public SettingsViewModel SettingsViewModel { get; }
        public MarketsViewModel MarketsViewModel { get; }
        public StrategiesViewModel StrategiesViewModel { get; }
        public ResearchViewModel ResearchViewModel { get; }
        public LogViewModel LogViewModel { get; }

        public static string Title => AboutModel.AssemblyTitle;

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
                Messenger.Send(new NotificationMessage(
                    Resources.SavingConfiguration), 0);

                string appData = MainService.GetAppDataFolder();
                if (!Directory.Exists(appData))
                {
                    Directory.CreateDirectory(appData);
                }

                SettingsViewModel.Save(Path.Combine(appData, "Settings.json"));
                MarketsViewModel.Save(Path.Combine(appData, "Markets.json"));
                StrategiesViewModel.Save(Path.Combine(appData, "Strategies.json"));
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
            string appData = MainService.GetAppDataFolder();

            if (update)
            {
                // Save model and initialize
                SettingsViewModel.Save(Path.Combine(appData, "Settings.json"));

                // Async initialize without blocking UI
                _initializer = Initialize();
            }
            else
            {
                // Reload old settings
                await SettingsViewModel.ReadAsync(Path.Combine(appData, "Settings.json")).ConfigureAwait(true);
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
                string program = MainService.GetProgramFolder();
                string appDataFolder = MainService.GetAppDataFolder();
                string programDataFolder = MainService.GetProgramDataFolder();
                string userDataFolder = MainService.GetUserDataFolder();
                Log.Trace($"Program folder: {program}");
                Log.Trace($"AppData folder: {appDataFolder}");
                Log.Trace($"ProgramData folder: {programDataFolder}");
                Log.Trace($"UserData folder: {userDataFolder}");
                MainService.Delete(Path.Combine(programDataFolder, "market-hours"));
                MainService.Delete(Path.Combine(programDataFolder, "symbol-properties"));
                MainService.DeleteFolders(appDataFolder, "temp*");
                MainService.CopyDirectory(
                    Path.Combine(program, "Content/AppData"),
                    appDataFolder,
                    false);
                MainService.CopyDirectory(
                    Path.Combine(program, "Content/ProgramData"),
                    programDataFolder,
                    false);
                MainService.CopyDirectory(
                    Path.Combine(program, "Content/UserData"),
                    userDataFolder,
                    false);

                // Read settings
                string appData = MainService.GetAppDataFolder();
                await SettingsViewModel.ReadAsync(Path.Combine(appData, "Settings.json")).ConfigureAwait(true);

                // Set config
                Config.Set("data-directory", SettingsViewModel.Model.DataFolder);
                Config.Set("data-folder", SettingsViewModel.Model.DataFolder);
                Config.Set("cache-location", SettingsViewModel.Model.DataFolder);
                Config.Set("map-file-provider",
                           "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");
                Globals.Reset();

                // Read configuration
                await MarketsViewModel.ReadAsync(Path.Combine(appData, "Markets.json"));
                await StrategiesViewModel.ReadAsync(Path.Combine(appData, "Strategies.json")).ConfigureAwait(true);

                // Update Data folder
                MainService.CopyDirectory(
                    Path.Combine(program, "Content/ProgramData/market-hours"),
                    Path.Combine(Globals.DataFolder, "market-hours"),
                    true);
                MainService.CopyDirectory(
                    Path.Combine(program, "Content/ProgramData/symbol-properties"),
                    Path.Combine(Globals.DataFolder, "symbol-properties"),
                    true);

                // Initialize Research page
                ResearchViewModel.Initialize();
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
