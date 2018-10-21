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
        private ViewModelBase _selectedItem;

        public StrategiesViewModel(StrategiesModel model, SettingsModel settingsModel)
        {
            Model = model;
            _settingsModel = settingsModel;

            AddCommand = new RelayCommand(() => AddStrategy(), true);
            DeleteCommand = new RelayCommand<StrategyViewModel>((strategy) => DeleteStrategy(strategy), (strategy) => strategy != null);
            SelectedChangedCommand = new RelayCommand<ViewModelBase>((strategy) => OnSelectedChanged(strategy), (strategy) => strategy != null);
            ImportCommand = new RelayCommand(() => ImportStrategy(), true);

            DataFromModel();
        }

        public StrategiesModel Model { get; }

        public SyncObservableCollection<StrategyViewModel> Strategies { get; } = new SyncObservableCollection<StrategyViewModel>();

        public RelayCommand AddCommand { get; }
        public RelayCommand<StrategyViewModel> DeleteCommand { get; }
        public RelayCommand<ViewModelBase> SelectedChangedCommand { get; }
        public RelayCommand ImportCommand { get; }

        public ViewModelBase SelectedItem
        {
            get => _selectedItem;
            set
            {
                Set(ref _selectedItem, value);
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnSelectedChanged(ViewModelBase strategy)
        {
            SelectedItem = strategy;
        }

        private void AddStrategy()
        {
            var strategy = new StrategyViewModel(this, new StrategyModel(), _settingsModel);
            Strategies.Add(strategy);
        }

        internal bool DeleteStrategy(StrategyViewModel strategy)
        {
            Debug.Assert(strategy != null);
            bool ok = Strategies.Remove(strategy);
            DataToModel();
            SelectedItem = null;
            return ok;
        }

        internal void CloneStrategy(StrategyViewModel strategyViewModel)
        {
            strategyViewModel.DataToModel();
            var strategyModel = new StrategyModel(strategyViewModel.Model);
            var strategy = new StrategyViewModel(this, strategyModel, _settingsModel);
            Strategies.Add(strategy);
        }

        internal bool Read(string fileName)
        {
            if (!File.Exists(fileName))
                return false;

            try
            {
                using (StreamReader r = new StreamReader(fileName))
                {
                    string json = r.ReadToEnd();
                    Model.Copy(JsonConvert.DeserializeObject<StrategiesModel>(json));
                }

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

        private void ImportStrategy()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "json file (*.json)|*.json|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    foreach (string fileName in openFileDialog.FileNames)
                    {
                        using (StreamReader r = new StreamReader(fileName))
                        {
                            string json = r.ReadToEnd();
                            StrategyModel strategy = JsonConvert.DeserializeObject<StrategyModel>(json);
                            foreach (StrategyJobModel job in strategy.Jobs)
                            {
                                job.Active = false;
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
            }
        }

        internal void ExportStrategy(StrategyViewModel strategyViewModel)
        {
            strategyViewModel.DataToModel();
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = strategyViewModel.Model.Name;
//            saveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            saveFileDialog.Filter = "json file (*.json)|*.json|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string fileName = saveFileDialog.FileName;
                    using (StreamWriter file = File.CreateText(fileName))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(file, strategyViewModel.Model);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().ToString());
                }
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
                foreach (StrategyJobViewModel job in strategy.Jobs)
                {
                    if (job.Active)
                    {
                        Task task = job.StartTaskAsync();
                    }
                }
            }
        }
    }
}
