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
            WeakReferenceMessenger.Default.Register<MainViewModel, NotificationMessage, int>(
                this, 0, static (r, m) => r.OnStatusMessage(m));

            Initialize();
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public RelayCommand SaveCommand { get; }
        public RelayCommand<Window> ExitCommand { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public MarketsViewModel MarketsViewModel { get; }
        public StrategiesViewModel StrategiesViewModel { get; }
        public ResearchViewModel ResearchViewModel { get; }
        public LogViewModel LogViewModel { get; }

        public static string Title => $"{AboutModel.Title} {AboutModel.Version}";

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
            IsBusy = true;
            try
            {
                string programData = MainService.GetProgramDataFolder();
                SettingsViewModel.Save(programData);
                MarketsViewModel.Save(programData);
                StrategiesViewModel.Save(programData);
                Messenger.Send(new NotificationMessage(Resources.ConfigurationSaved), 0);
            }
            catch (Exception ex)
            {
                Messenger.Send(new NotificationMessage(ex.Message), 0);
                Log.Error(ex);
            }
            IsBusy = false;
        }

        public void DoSettings(bool update)
        {
            Debug.Assert(IsUiThread(), "Not UI thread!");
            string programData = MainService.GetProgramDataFolder();
            try
            {
                if (update)
                {
                    // Save model and initialize
                    SettingsViewModel.Save(programData);
                    Initialize();
                }
                else
                {
                    // Reload old settings
                    if (SettingsViewModel.Read(programData))
                    {
                        Messenger.Send(new NotificationMessage(string.Empty), 0);
                    }
                    else
                    {
                        Messenger.Send(new NotificationMessage(Resources.SettingsReadFailed), 0);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex);
                Messenger.Send(new NotificationMessage(ex.Message), 0) ;
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

        private void Initialize()
        {
            IsBusy = true;
            try
            {
                // Set working directory
                string appData = MainService.GetAppDataFolder();
                Directory.CreateDirectory(appData);
                Directory.SetCurrentDirectory(appData);

                // Initialize data folders
                string programFolder = MainService.GetProgramFolder();
                string appDataFolder = MainService.GetAppDataFolder();
                string programDataFolder = MainService.GetProgramDataFolder();
                Log.Trace($"Program folder: {programFolder}");
                Log.Trace($"AppData folder: {appDataFolder}");
                Log.Trace($"ProgramData folder: {programDataFolder}");
                Directory.CreateDirectory(programDataFolder);

                // Cleanup temporary folder
                MainService.DeleteFiles(appDataFolder, "*");
                MainService.DeleteFolders(appDataFolder, "*");

                // Update Market data
                MainService.CopyDirectory(
                    Path.Combine(programFolder, "Content/ProgramData"),
                    programDataFolder,
                    false);

                // Read settings
                SettingsViewModel.Read(programDataFolder);

                // Set max backtests
                BacktestManager.SetSlots(SettingsViewModel.Model.MaxBacktests);

                // Set config
                Config.Set("data-directory", SettingsViewModel.Model.DataFolder);
                Config.Set("data-folder", SettingsViewModel.Model.DataFolder);
                Config.Set("cache-location", SettingsViewModel.Model.DataFolder);
                Config.Set("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");
                Config.Set("version-id", string.Empty);
                Globals.Reset();

                // Read configuration
                MarketsViewModel.Read(programDataFolder);
                StrategiesViewModel.Read(programDataFolder);
                Log.Trace($"Starting {AboutModel.Product} {AboutModel.Version}");

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
    }
}
