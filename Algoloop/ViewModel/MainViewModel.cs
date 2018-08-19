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
using Algoloop.Properties;
using Algoloop.Service;
using Algoloop.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;

namespace Algoloop.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IAppDomainService _appDomainService;
        private bool _isBusy;

        public RelayCommand SettingsCommand { get; }
        public RelayCommand<Window> ExitCommand { get; }
        public RelayCommand AboutCommand { get; }

        public LogViewModel LogViewModel { get; }
        public MarketsViewModel MarketsViewModel { get; }
        public AccountsViewModel AccountsViewModel { get; }
        public StrategiesViewModel StrategiesViewModel { get; }

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

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IAppDomainService appDomainService, MarketsViewModel marketsViewModel, AccountsViewModel accountsViewModel, StrategiesViewModel strategiesViewModel, LogViewModel logViewModel)
        {
            _appDomainService = appDomainService;
            MarketsViewModel = marketsViewModel;
            AccountsViewModel = accountsViewModel;
            StrategiesViewModel = strategiesViewModel;
            LogViewModel = logViewModel;

            SettingsCommand = new RelayCommand(() => DoSettings(), () => !IsBusy);
            ExitCommand = new RelayCommand<Window>(window => DoExit(window), window => !IsBusy);
            AboutCommand = new RelayCommand(() => DoAbout(), () => !IsBusy);

            ReadConfig();
        }

        ~MainViewModel()
        {
            SaveConfig();
        }

        private void DoExit(Window window)
        {
            Debug.Assert(window != null);

            IsBusy = true;
            window.Close();
            IsBusy = false;
        }

        private void DoSettings()
        {
            var settings = new SettingsView();
            if ((bool)settings.ShowDialog())
            {
                Settings.Default.Save();
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

            return Path.Combine(appData, company, product);
        }

        private void ReadConfig()
        {
            string appData = GetAppDataFolder();
            MarketsViewModel.Read(Path.Combine(appData, "Markets.json"));
            AccountsViewModel.Read(Path.Combine(appData, "Accounts.json"));
            StrategiesViewModel.Read(Path.Combine(appData, "Strategies.json"));
        }

        private void SaveConfig()
        {
            string appData = GetAppDataFolder();
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }

            MarketsViewModel.Save(Path.Combine(appData, "Markets.json"));
            AccountsViewModel.Save(Path.Combine(appData, "Accounts.json"));
            StrategiesViewModel.Save(Path.Combine(appData, "Strategies.json"));
        }
    }
}