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
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using QuantConnect.Logging;
using QuantConnect.Parameters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Algoloop.ViewModel
{
    public class StrategyViewModel : ViewModelBase
    {
        private readonly string[] exclude = new[] { "symbols", "resolution", "market", "startdate", "enddate", "cash" };

        private StrategiesViewModel _parent;
        private IList _selection;
        private readonly SettingsModel _settingsModel;

        public StrategyViewModel(StrategiesViewModel parent, StrategyModel model, SettingsModel settingsModel)
        {
            _parent = parent;
            Model = model;
            _settingsModel = settingsModel;

            SelectionChangedCommand = new RelayCommand<IList>(m => SelectionChanged(m));
            RunCommand = new RelayCommand(() => RunStrategy(), true);
            CloneCommand = new RelayCommand(() => _parent?.CloneStrategy(this), true);
            ExportCommand = new RelayCommand(() => _parent?.ExportStrategy(this), true);
            DeleteStrategyCommand = new RelayCommand(() => _parent?.DeleteStrategy(this), true);
            DeleteSelectedJobsCommand = new RelayCommand(() => DeleteSelectedJobs(), true);
            AddSymbolCommand = new RelayCommand(() => AddSymbol(), true);
            ImportSymbolsCommand = new RelayCommand(() => ImportSymbols(), true);
            Model.AlgorithmNameChanged += UpdateParametersFromModel;

            DataFromModel();
        }

        public StrategyModel Model { get; }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();

        public SyncObservableCollection<ParameterViewModel> Parameters { get; } = new SyncObservableCollection<ParameterViewModel>();

        public SyncObservableCollection<StrategyJobViewModel> Jobs { get; } = new SyncObservableCollection<StrategyJobViewModel>();

        public RelayCommand<IList> SelectionChangedCommand { get; }
        public RelayCommand RunCommand { get; }

        public RelayCommand CloneCommand { get; }

        public RelayCommand ExportCommand { get; }

        public RelayCommand DeleteStrategyCommand { get; }
        public RelayCommand DeleteSelectedJobsCommand { get; }

        public RelayCommand AddSymbolCommand { get; }

        public RelayCommand ActiveCommand { get; }

        public RelayCommand ImportSymbolsCommand { get; }

        internal bool DeleteSymbol(SymbolViewModel symbol)
        {
            bool ok = Symbols.Remove(symbol);
            Debug.Assert(ok);
            return ok;
        }

        internal bool DeleteJob(StrategyJobViewModel job)
        {
            bool ok = Jobs.Remove(job);
            Debug.Assert(ok);
            return ok;
        }

        private void SelectionChanged(IList selection)
        {
            _selection = selection;
        }

        internal void DeleteSelectedJobs()
        {
            if (_selection == null)
                return;

            foreach (object item in _selection)
            {
                

            }

        }

        internal void Refresh(SymbolViewModel symbolViewModel)
        {
        }

        private void AddSymbol()
        {
            var symbol = new SymbolViewModel(this, new SymbolModel());
            Symbols.Add(symbol);
        }

        private async void RunStrategy()
        {
            DataToModel();

            int count = 0;
            var models = GridOptimizerModels(Model, 0);
            int total = models.Count;
            var tasks = new List<Task>();
            using (var throttler = new SemaphoreSlim(Environment.ProcessorCount))
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

        internal void DataFromModel()
        {
            Symbols.Clear();
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                var symbolViewModel = new SymbolViewModel(this, symbolModel);
                Symbols.Add(symbolViewModel);
            }

            UpdateParametersFromModel(Model.AlgorithmName);

            Jobs.Clear();
            foreach (StrategyJobModel strategyJobModel in Model.Jobs)
            {
                var strategyJobViewModel = new StrategyJobViewModel(this, strategyJobModel, _settingsModel);
                Jobs.Add(strategyJobViewModel);
            }
        }

        private void ImportSymbols()
        {
            throw new NotImplementedException();
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
    }
}
