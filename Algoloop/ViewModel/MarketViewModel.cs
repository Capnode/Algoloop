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
using System.Threading;
using System.Threading.Tasks;

namespace Algoloop.ViewModel
{
    public class MarketViewModel : ViewModelBase
    {
        private readonly MarketsViewModel _parent;
        private readonly IAppDomainService _appDomainService;
        private CancellationTokenSource _cancel = new CancellationTokenSource();

        public MarketViewModel(MarketsViewModel marketsViewModel, MarketModel marketModel, IAppDomainService appDomainService)
        {
            _parent = marketsViewModel;
            Model = marketModel;
            _appDomainService = appDomainService;

            AddSymbolCommand = new RelayCommand(() => AddSymbol(), true);
            ImportSymbolsCommand = new RelayCommand(() => ImportSymbols(), true);
            DeleteMarketCommand = new RelayCommand(() => _parent?.DeleteMarket(this), true);
            EnabledCommand = new RelayCommand(() => ProcessMarket(Model.Enabled), true);

            DataFromModel();

            ProcessMarket(Model.Enabled);
        }

        ~MarketViewModel()
        {
        }

        public MarketModel Model { get; }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();

        public RelayCommand AddSymbolCommand { get; }

        public RelayCommand ImportSymbolsCommand { get; }

        public RelayCommand DeleteMarketCommand { get; }

        public RelayCommand EnabledCommand { get; }

        public bool Enabled
        {
            get => Model.Enabled;
            set
            {
                Model.Enabled = value;
                RaisePropertyChanged(() => Enabled);
            }
        }

        private void AddSymbol()
        {
            var symbol = new SymbolViewModel(this, new SymbolModel());
            Symbols.Add(symbol);
        }

        private void ImportSymbols()
        {
            throw new NotImplementedException();
        }

        internal bool DeleteSymbol(SymbolViewModel symbol)
        {
            return Symbols.Remove(symbol);
        }

        internal void DataToModel()
        {
            Model.Symbols.Clear();
            foreach (SymbolViewModel symbol in Symbols)
            {
                Model.Symbols.Add(symbol.Model);
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
        }
        private async void ProcessMarket(bool enabled)
        {
            if (enabled)
            {
                DataToModel();
                await Task.Run(() => _appDomainService.Run(Model), _cancel.Token);
                DataFromModel();
            }
        }
    }
}
