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
        private readonly SettingsModel _settingsModel;
        private CancellationTokenSource _cancel;
        private MarketModel _model;
        private Isolated<Toolbox> _toolbox;

        public MarketViewModel(MarketsViewModel marketsViewModel, MarketModel marketModel, SettingsModel settingsModel)
        {
            _parent = marketsViewModel;
            Model = marketModel;
            _settingsModel = settingsModel;

            AddSymbolCommand = new RelayCommand(() => AddSymbol(), true);
            ImportSymbolsCommand = new RelayCommand(() => ImportSymbols(), true);
            DeleteCommand = new RelayCommand(() => _parent?.DeleteMarket(this), () => !Enabled);
            EnableCommand = new RelayCommand(() => OnEnableCommand(Model.Enabled), true);
            StartCommand = new RelayCommand(() => OnStartCommand(), () => !Enabled);
            StopCommand = new RelayCommand(() => OnStopCommand(), () => Enabled);

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
        }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();

        public CollectionViewSource ActiveSymbols { get; } = new CollectionViewSource();

        public RelayCommand AddSymbolCommand { get; }

        public RelayCommand ImportSymbolsCommand { get; }

        public RelayCommand DeleteCommand { get; }

        public RelayCommand EnableCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }

        public bool Enabled
        {
            get => Model.Enabled;
            set
            {
                Model.Enabled = value;
                RaisePropertyChanged(() => Enabled);
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
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

        internal void DataToModel()
        {
            Model.DataFolder = _settingsModel.DataFolder;

            Model.Symbols.Clear();
            foreach (SymbolViewModel symbol in Symbols)
            {
                Model.Symbols.Add(symbol.Model);
            }
        }

        internal void DataFromModel()
        {
            Enabled = Model.Enabled;
            Symbols.Clear();
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                var symbolViewModel = new SymbolViewModel(this, symbolModel);
                Symbols.Add(symbolViewModel);
            }
        }

        internal bool DeleteSymbol(SymbolViewModel symbol)
        {
            return Symbols.Remove(symbol);
        }

        internal async Task StartTaskAsync()
        {
            DataToModel();
            MarketModel model = Model;
            _cancel = new CancellationTokenSource();
            while (!_cancel.Token.IsCancellationRequested && Model.Enabled)
            {
                Log.Trace($"{Model.Provider} download {Model.Resolution} {Model.FromDate:d}");
                try
                {
                    _toolbox = new Isolated<Toolbox>();
                    _cancel = new CancellationTokenSource();
                    await Task.Run(() => model = _toolbox.Value.Run(Model, new HostDomainLogger()), _cancel.Token);
                    _toolbox.Dispose();
                    _toolbox = null;
                }
                catch (AppDomainUnloadedException)
                {
                    Log.Trace($"Market {Model.Name} canceled by user");
                    _toolbox = null;
                    Enabled = false;
                }
                catch (Exception ex)
                {
                    Log.Trace($"{ex.GetType()}: {ex.Message}");
                    _toolbox.Dispose();
                    _toolbox = null;
                    Enabled = false;
                }

                // Update view
                Model = null;
                Model = model;
                DataFromModel();
            }

            Log.Trace($"{Model.Provider} download complete");
            _cancel = null;
        }

        private void StopTask()
        {
            if (_cancel != null)
            {
                _cancel.Cancel();
            }

            if (_toolbox != null)
            {
                _toolbox.Dispose();
            }
        }

        private async void OnEnableCommand(bool value)
        {
            if (value)
            {
                await StartTaskAsync();
            }
            else
            {
                StopTask();
            }
        }

        private async void OnStartCommand()
        {
            Enabled = true;
            await StartTaskAsync();
        }

        private void OnStopCommand()
        {
            StopTask();
            Enabled = false;
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
    }
}
