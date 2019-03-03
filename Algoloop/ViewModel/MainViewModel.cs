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
using System.Windows;
using Algoloop.Model;
using Algoloop.Provider;
using Algoloop.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using QuantConnect.Configuration;

namespace Algoloop.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private bool _isBusy;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(
            SettingsViewModel settingsViewModel,
            MarketsViewModel marketsViewModel,
            AccountsViewModel accountsViewModel,
            StrategiesViewModel strategiesViewModel,
            LogViewModel logViewModel)
        {
            SettingsViewModel = settingsViewModel;
            MarketsViewModel = marketsViewModel;
            AccountsViewModel = accountsViewModel;
            StrategiesViewModel = strategiesViewModel;
            LogViewModel = logViewModel;

            SettingsCommand = new RelayCommand(() => DoSettings(), () => !IsBusy);
            ExitCommand = new RelayCommand<Window>(window => DoExit(window), window => !IsBusy);
            AboutCommand = new RelayCommand(() => DoAbout(), () => !IsBusy);

            Config.Set("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");

            // Set working directory
            string appData = GetAppDataFolder();
            Directory.SetCurrentDirectory(appData);

            // Read configuration
            ReadConfig(appData);

            ProviderFactory.RegisterProviders();
        }

        public RelayCommand SettingsCommand { get; }
        public RelayCommand<Window> ExitCommand { get; }
        public RelayCommand AboutCommand { get; }

        public SettingsViewModel SettingsViewModel { get; }
        public LogViewModel LogViewModel { get; }
        public MarketsViewModel MarketsViewModel { get; }
        public AccountsViewModel AccountsViewModel { get; }
        public StrategiesViewModel StrategiesViewModel { get; }

        public string Title
        {
            get => AboutViewModel.AssemblyTitle;
        }

        /// <summary>
        /// Mark ongoing operation
        /// </summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                RaisePropertyChanged("IsBusy");
            }
        }

        public void SaveAll()
        {
            string appData = GetAppDataFolder();
            SaveConfig(appData);
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
            SettingsModel oldSettings = SettingsViewModel.Model;
            var settings = new SettingsView();
            if ((bool)settings.ShowDialog())
            {
                SaveAll();
            }
            else
            {
                SettingsViewModel.Model.Copy(oldSettings);
            }
        }

        private void DoAbout()
        {
            var about = new About();
            about.ShowDialog();
        }

        private string GetAppDataFolder()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            Assembly assembly = Assembly.GetEntryAssembly();
            string company = string.Empty;
            string product = string.Empty;
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

            string path = Path.Combine(appData, company, product);
            Directory.CreateDirectory(path);
            return path;
        }

        private void ReadConfig(string appData)
        {
            SettingsViewModel.Read(Path.Combine(appData, "Settings.json"));
            MarketsViewModel.Read(Path.Combine(appData, "Markets.json"));
            AccountsViewModel.Read(Path.Combine(appData, "Accounts.json"));
            StrategiesViewModel.Read(Path.Combine(appData, "Strategies.json"));
        }

        private void SaveConfig(string appData)
        {
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }

            SettingsViewModel.Save(Path.Combine(appData, "Settings.json"));
            MarketsViewModel.Save(Path.Combine(appData, "Markets.json"));
            AccountsViewModel.Save(Path.Combine(appData, "Accounts.json"));
            StrategiesViewModel.Save(Path.Combine(appData, "Strategies.json"));
        }
    }
}