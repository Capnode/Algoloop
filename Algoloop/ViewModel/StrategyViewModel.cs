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

using Algoloop.Common;
using Algoloop.Model;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using QuantConnect.Logging;
using QuantConnect.Parameters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Algoloop.ViewModel
{
    public class StrategyViewModel : ViewModelBase, ITreeViewModel
    {
        private readonly string[] exclude = new[] { "symbols", "resolution", "market", "startdate", "enddate", "cash" };
        private StrategiesViewModel _parent;
        private IList _taskSelection;
        private bool _isSelected;
        private bool _isExpanded;
        private SymbolViewModel _selectedSymbol;
        private ObservableCollection<DataGridColumn> _jobColumns = new ObservableCollection<DataGridColumn>();
        private StrategyJobViewModel _selectedJob;
        private readonly SettingsModel _settingsModel;

        public StrategyViewModel(StrategiesViewModel parent, StrategyModel model, SettingsModel settingsModel)
        {
            _parent = parent;
            Model = model;
            _settingsModel = settingsModel;

            StartCommand = new RelayCommand(() => RunStrategy(), true);
            StopCommand = new RelayCommand(() => { }, () => false);
            CloneCommand = new RelayCommand(() => _parent?.CloneStrategy(this), true);
            ExportCommand = new RelayCommand(() => _parent?.ExportStrategy(this), true);
            DeleteCommand = new RelayCommand(() => _parent?.DeleteStrategy(this), true);
//            DeleteAllJobsCommand = new RelayCommand(() => DeleteTasks(Summary.Rows), true);
            DeleteSelectedJobsCommand = new RelayCommand(() => DeleteTasks(_taskSelection), true);
            UseParametersCommand = new RelayCommand(() => UseParameters(_taskSelection), true);
            AddSymbolCommand = new RelayCommand(() => AddSymbol(), true);
            DeleteSymbolsCommand = new RelayCommand<IList>(m => DeleteSymbols(m), m => SelectedSymbol != null);
            ImportSymbolsCommand = new RelayCommand(() => ImportSymbols(), true);
            ExportSymbolsCommand = new RelayCommand<IList>(m => ExportSymbols(m), trm => SelectedSymbol != null);
            TaskSelectionChangedCommand = new RelayCommand<IList>(m => _taskSelection = m);
            TaskDoubleClickCommand = new RelayCommand<DataRowView>(m => OnSelectItem(m));

            Model.AlgorithmNameChanged += UpdateParametersFromModel;
            DataFromModel();
        }

        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand CloneCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand DeleteAllJobsCommand { get; }
        public RelayCommand DeleteSelectedJobsCommand { get; }
        public RelayCommand UseParametersCommand { get; }
        public RelayCommand AddSymbolCommand { get; }
        public RelayCommand<IList> DeleteSymbolsCommand { get; }
        public RelayCommand ActiveCommand { get; }
        public RelayCommand ImportSymbolsCommand { get; }
        public RelayCommand<IList> ExportSymbolsCommand { get; }
        public RelayCommand<IList> TaskSelectionChangedCommand { get; }
        public RelayCommand<DataRowView> TaskDoubleClickCommand { get; }

        public StrategyModel Model { get; }
        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();
        public SyncObservableCollection<ParameterViewModel> Parameters { get; } = new SyncObservableCollection<ParameterViewModel>();
        public SyncObservableCollection<StrategyJobViewModel> Jobs { get; } = new SyncObservableCollection<StrategyJobViewModel>();

        public bool IsSelected
        {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => Set(ref _isExpanded, value);
        }

        public ObservableCollection<DataGridColumn> JobColumns
        {
            get => _jobColumns;
            set => Set(ref _jobColumns, value);
        }

        public SymbolViewModel SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                Set(ref _selectedSymbol, value);
                DeleteSymbolsCommand.RaiseCanExecuteChanged();
                ExportSymbolsCommand.RaiseCanExecuteChanged();
            }
        }

        public StrategyJobViewModel SelectedJob
        {
            get => _selectedJob;
            set
            {
                Set(ref _selectedJob, value);
            }
        }

        public void Refresh()
        {
            Model.Refresh();
        }

        internal bool DeleteJob(StrategyJobViewModel job)
        {
            bool ok = Jobs.Remove(job);
            return ok;
        }

        internal static void AddPath(string path)
        {
            string pathValue = Environment.GetEnvironmentVariable("PATH");
            if (pathValue.Contains(path))
                return;

            pathValue += ";" + path;
            Environment.SetEnvironmentVariable("PATH", pathValue);
        }

        internal void UseParameters(StrategyJobViewModel job)
        {
            if (job == null)
                return;

            Parameters.Clear();
            foreach (ParameterViewModel parameter in job.Parameters)
            {
                Parameters.Add(parameter);
            }

            RemoveUnusedSummaryColumns();
        }

        internal void DataToModel()
        {
            Model.Symbols.Clear();
            foreach (SymbolViewModel symbol in Symbols)
            {
                Model.Symbols.Add(symbol.Model);
            }

            Model.Parameters.Clear();
            foreach (ParameterViewModel parameter in Parameters)
            {
                Model.Parameters.Add(parameter.Model);
            }

            Model.Jobs.Clear();
            foreach (StrategyJobViewModel job in Jobs)
            {
                Model.Jobs.Add(job.Model);
                job.DataToModel();
            }
        }

        internal DataRow CreateSummaryRow(StrategyJobViewModel task)
        {
            //            return Summary.CreateRow(task);
            return null;
        }

        internal void RemoveUnusedSummaryColumns()
        {
//            Summary.RemoveUnusedColumns();
            RefreshSummary();
        }

        internal void RefreshSummary()
        {
//            DataView = null;
//            DataView = Summary.DefaultView;
        }

        private void DeleteTasks(DataRowCollection rows)
        {
            var list = new DataRow[rows.Count];
            rows.CopyTo(list, 0);
            foreach (DataRow row in list)
            {
                StrategyJobViewModel job = row[0] as StrategyJobViewModel;
                if (job != null)
                {
                    job.DeleteJob();
                }

                row.Delete();
            }

            RemoveUnusedSummaryColumns();
        }

        private void DeleteTasks(IList rows)
        {
            List<DataRowView> list = rows?.Cast<DataRowView>()?.ToList();
            if (list == null)
                return;

            foreach (DataRowView row in list)
            {
                StrategyJobViewModel job = row[0] as StrategyJobViewModel;
                if (job != null)
                {
                    job.DeleteJob();
                }

                row.Delete();
            }

            RemoveUnusedSummaryColumns();
        }

        private void OnSelectItem(DataRowView row)
        {
            if (row == null)
                return;

            StrategyJobViewModel job = row[0] as StrategyJobViewModel;
            if (job == null)
                return;

            job.IsSelected = true;
            _parent.SelectedItem = job;
            IsExpanded = true;
        }

        private void AddSymbol()
        {
            var symbol = new SymbolViewModel(this, new SymbolModel());
            Symbols.Add(symbol);
        }

        private void UseParameters(IList selected)
        {
            if (selected == null)
                return;

            List<DataRowView> list = selected.Cast<DataRowView>().ToList();
            DataRowView row = list.FirstOrDefault();
            UseParameters(row[0] as StrategyJobViewModel);
        }

        private async void RunStrategy()
        {
            DataToModel();

            int count = 0;
            var models = GridOptimizerModels(Model, 0);
            int total = models.Count;
            var tasks = new List<Task>();
            using (var throttler = new SemaphoreSlim(_settingsModel.MaxBacktests))
            {
                foreach (StrategyModel model in models)
                {
                    await throttler.WaitAsync();
                    count++;
                    var jobModel = new StrategyJobModel(model.AlgorithmName, model);
                    Log.Trace($"Strategy {jobModel.AlgorithmName} {jobModel.Name} {count}({total})");
                    var job = new StrategyJobViewModel(this, jobModel, _settingsModel);
                    Jobs.Add(job);
                    Task task =  job.StartTaskAsync().ContinueWith(m => { throttler.Release(); });
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
        }

        private List<StrategyModel> GridOptimizerModels(StrategyModel rawModel, int index)
        {
            var model = new StrategyModel(rawModel);

            var list = new List<StrategyModel>();
            if (index < model.Parameters.Count)
            {
                var parameter = model.Parameters[index];
                if (parameter.UseRange)
                {
                    foreach (string value in SplitRange(parameter.Range))
                    {
                        parameter.Value = value;
                        parameter.UseValue = true;
                        list.AddRange(GridOptimizerModels(model, index + 1));
                    }
                }
                else
                {
                    list.AddRange(GridOptimizerModels(model, index + 1));
                }
            }
            else
            {
                list.Add(model);
            }

            return list;
        }

        private IEnumerable<string> SplitRange(string range)
        {
            foreach (string value in range.Split(','))
            {
                yield return value;
            }
        }

        private void DataFromModel()
        {
            Symbols.Clear();
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                var symbolViewModel = new SymbolViewModel(this, symbolModel);
                Symbols.Add(symbolViewModel);
            }

            UpdateParametersFromModel(Model.AlgorithmName);

            UpdateJobsAndColumns();
        }

        private void UpdateJobsAndColumns()
        {
            Jobs.Clear();

            JobColumns.Clear();
            JobColumns.Add(new DataGridCheckBoxColumn()
            {
                Header = "Active",
                Binding = new Binding("Active") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
            });
            JobColumns.Add(new DataGridTextColumn()
            {
                Header = "Name",
                Binding = new Binding("Model.Name") { Mode = BindingMode.OneWay}
            });

            foreach (StrategyJobModel strategyJobModel in Model.Jobs)
            {
                var strategyJobViewModel = new StrategyJobViewModel(this, strategyJobModel, _settingsModel);
                Jobs.Add(strategyJobViewModel);
                ExDataGridColumns.AddPropertyColumns(JobColumns, strategyJobViewModel.Statistics);
            }
        }

        private void UpdateParametersFromModel(string algorithmName)
        {
            if (string.IsNullOrEmpty(Model.AlgorithmLocation) || string.IsNullOrEmpty(algorithmName))
                return;

            Assembly assembly = Assembly.LoadFrom(Model.AlgorithmLocation);
            if (assembly == null)
                return;

            IEnumerable<Type> type = assembly.GetTypes().Where(m => m.Name.Equals(algorithmName));
            if (type == null || type.Count() == 0)
                return;

            Parameters.Clear();
            foreach (KeyValuePair<string, string> parameter in ParameterAttribute.GetParametersFromType(type.First()))
            {
                string parameterName = parameter.Key;
                string parameterType = parameter.Value;

                if (exclude.Contains(parameterName))
                    continue;

                ParameterModel parameterModel = Model.Parameters.FirstOrDefault(m => m.Name.Equals(parameterName));
                if (parameterModel == null)
                {
                    parameterModel = new ParameterModel() { Name = parameterName };
                }

                var parameterViewModel = new ParameterViewModel(this, parameterModel);
                Parameters.Add(parameterViewModel);
            }

            RaisePropertyChanged(() => Parameters);
        }

        private void DeleteSymbols(IList symbols)
        {
            Debug.Assert(symbols != null);
            if (Symbols.Count == 0 || symbols.Count == 0)
                return;

            // Create a copy of the list before remove
            List<SymbolViewModel> list = symbols.Cast<SymbolViewModel>()?.ToList();
            Debug.Assert(list != null);

            int pos = Symbols.IndexOf(list.First());
            foreach (SymbolViewModel symbol in list)
            {
                Symbols.Remove(symbol);
            }

            DataToModel();
            if (Symbols.Count > 0)
            {
                SelectedSymbol = Symbols[Math.Min(pos, Symbols.Count - 1)];
            }
        }

        private void ImportSymbols()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "symbol file (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == false)
                return;

            try
            {
                foreach (string fileName in openFileDialog.FileNames)
                {
                    using (StreamReader r = new StreamReader(fileName))
                    {
                        while (!r.EndOfStream)
                        {
                            string name = r.ReadLine();
                            if (!Model.Symbols.Exists(m => m.Name.Equals(name)))
                            {
                                var symbol = new SymbolModel() { Name = name };
                                Model.Symbols.Add(symbol);
                            }
                        }
                    }
                }

                DataFromModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
            }
        }

        private void ExportSymbols(IList symbols)
        {
            Debug.Assert(symbols != null);
            if (Symbols.Count == 0 || symbols.Count == 0)
                return;

            DataToModel();
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "symbol file (*.txt)|*.txt|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == false)
                return;

            try
            {
                string fileName = saveFileDialog.FileName;
                using (StreamWriter file = File.CreateText(fileName))
                {
                    foreach (SymbolViewModel symbol in symbols)
                    {
                        file.WriteLine(symbol.Model.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
            }
        }
    }
}
