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
using Algoloop.Lean.Service;
using Algoloop.Lean.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;

namespace Algoloop.Lean.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly ILeanEngineService _leanEngineService;
        private bool _isBusy;
        private string _fileName;

        public RelayCommand OpenCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand SaveAsCommand { get; }
        public RelayCommand<Window> ExitCommand { get; }
        public RelayCommand AboutCommand { get; }
        public RelayCommand RunCommand { get; }

        public LogViewModel LogViewModel { get; }
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
                RunCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Stategies filename
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                RaisePropertyChanged("FileName");
                OpenCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
                SaveAsCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(ILeanEngineService leanEngineService, LogViewModel logViewModel, AccountsViewModel loginsViewModel, StrategiesViewModel strategiesViewModel)
        {
            _leanEngineService = leanEngineService;
            LogViewModel = logViewModel;
            AccountsViewModel = loginsViewModel;
            StrategiesViewModel = strategiesViewModel;

            OpenCommand = new RelayCommand(() => OpenFile(), () => !IsBusy);
            SaveCommand = new RelayCommand(() => SaveFile(), () => !IsBusy && !string.IsNullOrEmpty(FileName) );
            SaveAsCommand = new RelayCommand(() => SaveAsFile(), () => !IsBusy);
            ExitCommand = new RelayCommand<Window>(window => Close(window), window => !IsBusy);
            AboutCommand = new RelayCommand(() => About(), () => !IsBusy);
            RunCommand = new RelayCommand(() => RunBacktest(), () => !IsBusy);

            ReadConfig();
        }

        ~MainViewModel()
        {
            SaveConfig();
        }

        private void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = @"c:\temp\";
            openFileDialog.Filter = "Algoloop file (*.alp)|*.alp|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                FileName = openFileDialog.FileName;
                StrategiesViewModel.Read(FileName);
            }
        }

        private void SaveFile()
        {
            Debug.Assert(!string.IsNullOrEmpty(FileName));
            StrategiesViewModel.Save(FileName);
        }

        private void SaveAsFile()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = @"c:\temp\";
            saveFileDialog.Filter = "Algoloop file (*.alp)|*.alp|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                FileName = saveFileDialog.FileName;
                StrategiesViewModel.Save(FileName);
            }
        }

        private void Close(Window window)
        {
            Debug.Assert(window != null);

            IsBusy = true;
            window.Close();
            IsBusy = false;
        }

        private void About()
        {
            var about = new About();
            about.ShowDialog();
        }

        private void RunBacktest()
        {
            IsBusy = true;
            IsBusy = false;
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

            AccountsViewModel.Save(Path.Combine(appData, "Accounts.json"));
            StrategiesViewModel.Save(Path.Combine(appData, "Strategies.json"));
        }
    }
}