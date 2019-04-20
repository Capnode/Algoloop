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
using System.Threading.Tasks;
using System.Windows;
using Algoloop.Model;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Algoloop.ViewModel
{
    public class StrategiesViewModel : ViewModelBase
    {
        private readonly SettingsModel _settingsModel;
        private ITreeViewModel _selectedItem;
        private bool _isBusy;

        public StrategiesViewModel(StrategiesModel model, SettingsModel settingsModel)
        {
            Model = model;
            _settingsModel = settingsModel;

            AddCommand = new RelayCommand(() => DoAddStrategy(), () => !IsBusy);
            ImportCommand = new RelayCommand(() => DoImportStrategies(), () => !IsBusy);
            ExportCommand = new RelayCommand(() => DoExportStrategies(), () => !IsBusy);
            SelectedChangedCommand = new RelayCommand<ITreeViewModel>((vm) => DoSelectedChanged(vm), true);

            DataFromModel();
        }

        public RelayCommand<ITreeViewModel> SelectedChangedCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand ImportCommand { get; }
        public RelayCommand ExportCommand { get; }

        public StrategiesModel Model { get; set; }
        public SyncObservableCollection<StrategyViewModel> Strategies { get; } = new SyncObservableCollection<StrategyViewModel>();

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
            if (!File.Exists(fileName))
                return false;

            try
            {
                await Task.Run(() =>
                {
                    using (StreamReader r = new StreamReader(fileName))
                    {
                        using (JsonReader reader = new JsonTextReader(r))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            Model = serializer.Deserialize<StrategiesModel>(reader);
                        }
                    }
                });

                DataFromModel();
                StartTasks();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
                return false;
            }
        }

        internal bool Save(string fileName)
        {
            try
            {
                DataToModel();

                using (StreamWriter file = File.CreateText(fileName))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, Model);
                    return true;
                }
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
                var strategy = new StrategyViewModel(this, new StrategyModel(), _settingsModel);
                Strategies.Add(strategy);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoSelectedChanged(ITreeViewModel vm)
        {
            try
            {
                IsBusy = true;
                if (vm != null)
                    vm.Refresh();

                SelectedItem = vm;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoImportStrategies()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "json file (*.json)|*.json|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() != true)
                return;

            try
            {
                IsBusy = true;
                foreach (string fileName in openFileDialog.FileNames)
                {
                    using (StreamReader r = new StreamReader(fileName))
                    {
                        string json = r.ReadToEnd();
                        StrategiesModel strategies = JsonConvert.DeserializeObject<StrategiesModel>(json);
                        foreach (StrategyModel strategy in strategies.Strategies)
                        {
                            foreach (TrackModel track in strategy.Tracks)
                            {
                                track.Active = false;
                            }

                            Model.Strategies.Add(strategy);
                        }
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
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            saveFileDialog.Filter = "json file (*.json)|*.json|All files (*.*)|*.*";
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
            foreach (StrategyViewModel strategy in Strategies)
            {
                Model.Strategies.Add(strategy.Model);
                strategy.DataToModel();
            }
        }

        private void DataFromModel()
        {
            Strategies.Clear();
            foreach (StrategyModel strategyModel in Model.Strategies)
            {
                var strategyViewModel = new StrategyViewModel(this, strategyModel, _settingsModel);
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
                        Task task = track.StartTaskAsync();
                    }
                }
            }
        }
    }
}
