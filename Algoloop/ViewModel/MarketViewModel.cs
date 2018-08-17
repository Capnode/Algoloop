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
using QuantConnect.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Algoloop.ViewModel
{
    public class MarketViewModel : ViewModelBase
    {
        private readonly MarketsViewModel _parent;
        private readonly IAppDomainService _appDomainService;
        private CancellationTokenSource _cancel;
        private MarketModel _model;

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

            ActiveSymbols.Filter += (object sender, FilterEventArgs e) =>
            {
                SymbolViewModel symbol = e.Item as SymbolViewModel;
                if (symbol != null)
                {
                    e.Accepted = symbol.Model.Enabled;
                }
            };

            ActiveSymbols.Source = Symbols;

            ProcessMarket(Model.Enabled);
        }

        ~MarketViewModel()
        {
        }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();

        public CollectionViewSource ActiveSymbols { get; } = new CollectionViewSource();

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

        public MarketModel Model
        {
            get => _model;
            set => Set(ref _model, value);
        }

        internal void Refresh(SymbolViewModel symbolViewModel)
        {
            ActiveSymbols.View.Refresh();
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
                _cancel = new CancellationTokenSource();
                DataToModel();
                Model.FromDate = new DateTime(Model.FromDate.Year, Model.FromDate.Month, Model.FromDate.Day); // Remove time part
                while (!_cancel.Token.IsCancellationRequested && Model.FromDate < DateTime.Now)
                {
                    Log.Trace($"{Model.Provider} download {Model.Resolution} {Model.FromDate:d}");
                    await Task.Run(() => _appDomainService.Run(Model), _cancel.Token);
                    if (!Model.Completed)
                    {
                        Log.Trace($"{Model.Provider} download {Model.Resolution} {Model.FromDate:d} failed");
                    }

                    if (Model.FromDate >= DateTime.Today)
                    {
                        break;
                    }

                    Model.FromDate = Model.FromDate.AddDays(1);

                    // Update view
                    MarketModel model = Model;
                    Model = null;
                    Model = model;
                }

                DataFromModel();
                Log.Trace($"{Model.Provider} download complete");
                _cancel = null;
                Enabled = false;
            }
            else
            {
                if (_cancel != null)
                {
                    _cancel.Cancel();
                }
            }
        }
    }
}
