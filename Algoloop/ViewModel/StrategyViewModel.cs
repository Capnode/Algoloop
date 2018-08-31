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
using Algoloop.Service;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Diagnostics;

namespace Algoloop.ViewModel
{
    public class StrategyViewModel : ViewModelBase
    {
        private StrategiesViewModel _parent;
        private IAppDomainService _appDomainService;

        public StrategyViewModel(StrategiesViewModel parent, StrategyModel model, IAppDomainService appDomainService)
        {
            _parent = parent;
            Model = model;
            _appDomainService = appDomainService;

            RunCommand = new RelayCommand(() => RunStrategy(), true);
            CloneCommand = new RelayCommand(() => _parent?.CloneStrategy(this), true);
            ExportCommand = new RelayCommand(() => _parent?.ExportStrategy(this), true);
            DeleteCommand = new RelayCommand(() => _parent?.DeleteStrategy(this), true);
            AddSymbolCommand = new RelayCommand(() => AddSymbol(), true);
            ImportSymbolsCommand = new RelayCommand(() => ImportSymbols(), true);
            AddParameterCommand = new RelayCommand(() => AddParameter(), true);

            DataFromModel();
        }

        public StrategyModel Model { get; }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();

        public SyncObservableCollection<ParameterViewModel> Parameters { get; } = new SyncObservableCollection<ParameterViewModel>();

        public SyncObservableCollection<StrategyJobViewModel> Jobs { get; } = new SyncObservableCollection<StrategyJobViewModel>();

        public RelayCommand RunCommand { get; }

        public RelayCommand CloneCommand { get; }

        public RelayCommand ExportCommand { get; }

        public RelayCommand DeleteCommand { get; }

        public RelayCommand AddSymbolCommand { get; }

        public RelayCommand EnabledCommand { get; }

        public RelayCommand ImportSymbolsCommand { get; }

        public RelayCommand AddParameterCommand { get; }

        internal bool DeleteSymbol(SymbolViewModel symbol)
        {
            bool ok = Symbols.Remove(symbol);
            Debug.Assert(ok);
            return ok;
        }

        internal void DeleteParameter(ParameterViewModel parameter)
        {
            Parameters.Remove(parameter);
        }

        internal bool DeleteJob(StrategyJobViewModel job)
        {
            bool ok = Jobs.Remove(job);
            Debug.Assert(ok);
            return ok;
        }

        internal void Refresh(SymbolViewModel symbolViewModel)
        {
        }

        private void AddSymbol()
        {
            var symbol = new SymbolViewModel(this, new SymbolModel());
            Symbols.Add(symbol);
        }

        private void AddParameter()
        {
            var parameter = new ParameterViewModel(this, new ParameterModel());
            Parameters.Add(parameter);
        }

        private async void RunStrategy()
        {
            DataToModel();
            var job = new StrategyJobViewModel(this, new StrategyJobModel(Model.AlgorithmName, Model), _appDomainService);
            Jobs.Add(job);
            await job.StartTaskAsync();
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

            Parameters.Clear();
            foreach (ParameterModel parameterModel in Model.Parameters)
            {
                var parameterViewModel = new ParameterViewModel(this, parameterModel);
                Parameters.Add(parameterViewModel);
            }

            Jobs.Clear();
            foreach (StrategyJobModel strategyJobModel in Model.Jobs)
            {
                var strategyJobViewModel = new StrategyJobViewModel(this, strategyJobModel, _appDomainService);
                Jobs.Add(strategyJobViewModel);
            }
        }

        private void ImportSymbols()
        {
            throw new NotImplementedException();
        }
    }
}
