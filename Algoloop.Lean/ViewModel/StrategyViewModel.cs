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

using Algoloop.Lean.Model;
using Algoloop.Lean.Service;
using Algoloop.Lean.ViewSupport;
using GalaSoft.MvvmLight.Command;
using System;
using System.Diagnostics;

namespace Algoloop.Lean.ViewModel
{
    public class StrategyViewModel
    {
        private StrategiesViewModel _parent;
        private ILeanEngineService _leanEngineService;

        public StrategyModel Model { get; }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();

        public SyncObservableCollection<ParameterViewModel> Parameters { get; } = new SyncObservableCollection<ParameterViewModel>();

        public SyncObservableCollection<StrategyJobViewModel> Jobs { get; } = new SyncObservableCollection<StrategyJobViewModel>();

        public RelayCommand DeleteStrategyCommand { get; }

        public RelayCommand AddSymbolCommand { get; }

        public RelayCommand RunBacktestCommand { get; }

        public RelayCommand ImportSymbolsCommand { get; }

        public RelayCommand AddParameterCommand { get; }

        public StrategyViewModel(StrategiesViewModel parent, StrategyModel model, ILeanEngineService leanEngineService)
        {
            _parent = parent;
            Model = model;
            _leanEngineService = leanEngineService;

            DeleteStrategyCommand = new RelayCommand(() => _parent?.DeleteStrategy(this), true);
            AddSymbolCommand = new RelayCommand(() => AddSymbol(), true);
            ImportSymbolsCommand = new RelayCommand(() => ImportSymbols(), true);
            RunBacktestCommand = new RelayCommand(() => RunBacktest(), true);
            AddParameterCommand = new RelayCommand(() => AddParameter(), true);

            DataFromModel();
        }

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

        private void RunBacktest()
        {
            DataToModel();
            var job = new StrategyJobViewModel(this, new StrategyJobModel("Backtest", Model), _leanEngineService);
            Jobs.Add(job);
            job.Start();
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
                var strategyJobViewModel = new StrategyJobViewModel(this, strategyJobModel, _leanEngineService);
                Jobs.Add(strategyJobViewModel);
            }
        }

        private void ImportSymbols()
        {
            throw new NotImplementedException();
        }
    }
}
