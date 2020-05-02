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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Algoloop.Model;
using Algoloop.Service;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Newtonsoft.Json;
using QuantConnect.Logging;

namespace Algoloop.ViewModel
{
    public class StrategiesViewModel : ViewModelBase
    {
        private readonly MarketService _markets;
        private readonly AccountService _accounts;
        private readonly SettingService _settings;

        private ITreeViewModel _selectedItem;
        private bool _isBusy;

        public StrategiesViewModel(StrategyService strategies, MarketService markets, AccountService accounts, SettingService settings)
        {
            Model = strategies;
            _markets = markets;
            _accounts = accounts;
            _settings = settings;

            AddCommand = new RelayCommand(() => DoAddStrategy(), () => !IsBusy);
            ImportCommand = new RelayCommand(() => DoImportStrategies(), () => !IsBusy);
            ExportCommand = new RelayCommand(() => DoExportStrategies(), () => !IsBusy);
            SelectedChangedCommand = new RelayCommand<ITreeViewModel>((vm) => DoSelectedChanged(vm), (vm) => !IsBusy);

            DataFromModel();
        }

        public RelayCommand<ITreeViewModel> SelectedChangedCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand ImportCommand { get; }
        public RelayCommand ExportCommand { get; }

        public StrategyService Model { get; set; }


        public SyncObservableCollection<StrategyViewModel> Strategies { get; } = new SyncObservableCollection<StrategyViewModel>();

        /// <summary>
        /// Mark ongoing operation
        /// </summary>
        public bool IsBusy
        {
            get =>_isBusy;
            set => Set(ref _isBusy, value);
        }

        public ITreeViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                Set(ref _selectedItem, value);
            }
        }

        internal void DoDeleteStrategy(StrategyViewModel strategy)
        {
            Debug.Assert(strategy != null);
            try
            {
                IsBusy = true;
                int pos = Strategies.IndexOf(strategy);
                Strategies.RemoveAt(pos);
                DataToModel();
                if (Strategies.Count == 0)
                {
                    SelectedItem = null;
                    return;
                }

                SelectedItem = Strategies[Math.Max(0, pos - 1)];
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal async Task<bool> ReadAsync(string fileName)
        {
            Log.Trace($"Reading {fileName}");
            if (File.Exists(fileName))
            {
                try
                {
                    await Task.Run(() =>
                    {
                        using StreamReader r = new StreamReader(fileName);
                        using JsonReader reader = new JsonTextReader(r);
                        JsonSerializer serializer = new JsonSerializer();
                        Model = serializer.Deserialize<StrategyService>(reader);
                    }).ConfigureAwait(true); // Must continue on UI thread

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().ToString());
                    return false;
                }
            }

            DataFromModel();
            StartTasks();
            return true;
        }

        internal bool Save(string fileName)
        {
            try
            {
                DataToModel();

                using StreamWriter file = File.CreateText(fileName);
                JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.Indented };
                serializer.Serialize(file, Model);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
                return false;
            }
        }

        private void DoAddStrategy()
        {
            try
            {
                IsBusy = true;
                var strategy = new StrategyViewModel(this, new StrategyModel(), _markets, _accounts, _settings);
                Strategies.Add(strategy);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoSelectedChanged(ITreeViewModel vm)
        {
            // No IsBusy here
            vm?.Refresh();
            SelectedItem = vm;
        }

        private void DoImportStrategies()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                Multiselect = true,
                Filter = "json file (*.json)|*.json|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() != true)
                return;

            try
            {
                IsBusy = true;
                foreach (string fileName in openFileDialog.FileNames)
                {
                    using StreamReader r = new StreamReader(fileName);
                    string json = r.ReadToEnd();
                    StrategyService strategies = JsonConvert.DeserializeObject<StrategyService>(json);
                    foreach (StrategyModel strategy in strategies.Strategies)
                    {
                        foreach (TrackModel track in strategy.Tracks)
                        {
                            track.Active = false;
                        }

                        Model.Strategies.Add(strategy);
                    }
                }

                DataFromModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoExportStrategies()
        {
            DataToModel();
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = "json file (*.json)|*.json|All files (*.*)|*.*"
            };
            if (saveFileDialog.ShowDialog() == false)
                return;

            try
            {
                IsBusy = true;
                Save(saveFileDialog.FileName);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DataToModel()
        {
            Model.Strategies.Clear();
            string folder = Directory.GetCurrentDirectory();
            var inUse = new List<string>();
            foreach (StrategyViewModel strategy in Strategies)
            {
                Model.Strategies.Add(strategy.Model);
                strategy.DataToModel();

                // Collect files in use
                inUse.AddRange(strategy.Model.Tracks
                    .Where(m => !string.IsNullOrEmpty(m.ZipFile))
                    .Select(p => Path.Combine(folder, p.ZipFile)));
            }

            // Remove Track files not in use
            DirectoryInfo dir = new DirectoryInfo(TrackViewModel.Folder);
            if (!dir.Exists)
            {
                return;
            }

            foreach (FileInfo file in dir.GetFiles())
            {
                if (!inUse.Contains(file.FullName))
                {
                    File.Delete(file.FullName);
                }
            }
        }

        private void DataFromModel()
        {
            Strategies.Clear();
            foreach (StrategyModel strategyModel in Model.Strategies)
            {
                var strategyViewModel = new StrategyViewModel(this, strategyModel, _markets, _accounts, _settings);
                Strategies.Add(strategyViewModel);
            }
        }

        private void StartTasks()
        {
            foreach (StrategyViewModel strategy in Strategies)
            {
                foreach (TrackViewModel track in strategy.Tracks)
                {
                    if (track.Active)
                    {
                        _ = track.StartTaskAsync();
                    }
                }
            }
        }
    }
}
