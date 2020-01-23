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

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Algoloop.Model;
using Algoloop.Properties;
using Algoloop.Provider;
using Algoloop.Service;
using Algoloop.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using QuantConnect.Configuration;
using QuantConnect.Logging;

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
            AccountsViewModel accountsViewModel,
            StrategiesViewModel strategiesViewModel,
            ResearchViewModel researchViewModel,
            LogViewModel logViewModel)
        {
            SettingsViewModel = settingsViewModel;
            MarketsViewModel = marketsViewModel;
            AccountsViewModel = accountsViewModel;
            StrategiesViewModel = strategiesViewModel;
            ResearchViewModel = researchViewModel;
            LogViewModel = logViewModel;

            SaveCommand = new RelayCommand(() => SaveAll(), () => !IsBusy);
            SettingsCommand = new RelayCommand(() => DoSettings(), () => !IsBusy);
            ExitCommand = new RelayCommand<Window>(window => DoExit(window), window => !IsBusy);
            Messenger.Default.Register<NotificationMessage>(this, OnStatusMessage);

            Config.Set("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");

            // Set working directory
            string appData = GetAppDataFolder();
            Directory.SetCurrentDirectory(appData);

            ProviderFactory.RegisterProviders();

            // Read configuration
            _ = ReadConfigAsync(appData);
        }

        public RelayCommand SaveCommand { get; }
        public RelayCommand SettingsCommand { get; }
        public RelayCommand<Window> ExitCommand { get; }

        public SettingsViewModel SettingsViewModel { get; }
        public MarketsViewModel MarketsViewModel { get; }
        public AccountsViewModel AccountsViewModel { get; }
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
            set => Set(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        public void SaveAll()
        {
            string appData = GetAppDataFolder();
            SaveConfig(appData);
        }

        public static string GetAppDataFolder()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            GetAssemblyInfo(out string company, out string product);
            string path = Path.Combine(appData, company, product);
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetUserDataFolder()
        {
            string userData = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            GetAssemblyInfo(out string company, out string product);
            string path = Path.Combine(userData, company, product);
            Directory.CreateDirectory(path);
            return path;
        }

        private static void GetAssemblyInfo(out string company, out string product)
        {
            Assembly assembly = Assembly.GetEntryAssembly();
            company = string.Empty;
            product = string.Empty;
            object[] companyAttributes = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            if ((companyAttributes != null) && (companyAttributes.Length > 0))
            {
                company = ((AssemblyCompanyAttribute)companyAttributes[0]).Company.Split(' ')[0];
            }

            object[] productAttributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            if ((productAttributes != null) && (productAttributes.Length > 0))
            {
                product = ((AssemblyProductAttribute)productAttributes[0]).Product;
            }
        }

        private void OnStatusMessage(NotificationMessage message)
        {
            StatusMessage = message.Notification;
            if (string.IsNullOrWhiteSpace(message.Notification))
                return;

            Log.Trace(message.Notification);
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

        private void DoSettings()
        {
            SettingService oldSettings = SettingsViewModel.Model;
            var settings = new SettingsView();
            if ((bool)settings.ShowDialog())
            {
                SaveAll();
                ResearchViewModel.StartJupyter();
            }
            else
            {
                SettingsViewModel.Model.Copy(oldSettings);
            }
        }

        private async Task ReadConfigAsync(string appData)
        {
            try
            {
                IsBusy = true;
                Messenger.Default.Send(new NotificationMessage(Resources.LoadingConfiguration));
                SettingsViewModel.Read(Path.Combine(appData, "Settings.json"));
                MarketsViewModel.Read(Path.Combine(appData, "Markets.json"));
                AccountsViewModel.Read(Path.Combine(appData, "Accounts.json"));
                await StrategiesViewModel.ReadAsync(Path.Combine(appData, "Strategies.json")).ConfigureAwait(true);
                ResearchViewModel.StartJupyter();
            }
            finally
            {
                Messenger.Default.Send(new NotificationMessage(string.Empty));
                IsBusy = false;
            }
        }

        private void SaveConfig(string appData)
        {
            try
            {
                IsBusy = true;
                Messenger.Default.Send(new NotificationMessage(Resources.SavingConfiguration));

                if (!Directory.Exists(appData))
                {
                    Directory.CreateDirectory(appData);
                }

                SettingsViewModel.Save(Path.Combine(appData, "Settings.json"));
                MarketsViewModel.Save(Path.Combine(appData, "Markets.json"));
                AccountsViewModel.Save(Path.Combine(appData, "Accounts.json"));
                StrategiesViewModel.Save(Path.Combine(appData, "Strategies.json"));
            }
            finally
            {
                Messenger.Default.Send(new NotificationMessage(string.Empty));
                IsBusy = false;
            }
        }
    }
}