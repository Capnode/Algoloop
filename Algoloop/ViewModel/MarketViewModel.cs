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
using System.Linq;
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
        private AppDomain _appDomain;

        public MarketViewModel(MarketsViewModel marketsViewModel, MarketModel marketModel, IAppDomainService appDomainService)
        {
            _parent = marketsViewModel;
            Model = marketModel;
            _appDomainService = appDomainService;

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

        public string Logs
        {
            get => Model.Logs;
        }

        public int Loglines
        {
            get => Logs == null ? 0 : Logs.Count(m => m.Equals('\n'));
        }

        internal void Refresh(SymbolViewModel symbolViewModel)
        {
            ActiveSymbols.View.Refresh();
        }

        internal void DataToModel()
        {
            Model.DataFolder = Properties.Settings.Default.DataFolder;
            Model.Logs = string.Empty;

            Model.Symbols.Clear();
            foreach (SymbolViewModel symbol in Symbols)
            {
                Model.Symbols.Add(symbol.Model);
            }

            RaisePropertyChanged(() => Logs);
            RaisePropertyChanged(() => Loglines);
        }

        internal void DataFromModel()
        {
            Symbols.Clear();
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                var symbolViewModel = new SymbolViewModel(this, symbolModel);
                Symbols.Add(symbolViewModel);
            }

            RaisePropertyChanged(() => Logs);
            RaisePropertyChanged(() => Loglines);
        }

        internal bool DeleteSymbol(SymbolViewModel symbol)
        {
            return Symbols.Remove(symbol);
        }

        internal async Task StartTaskAsync()
        {
            _cancel = new CancellationTokenSource();
            DataToModel();
            Model.FromDate = Model.FromDate.Date; // Remove time part
            while (!_cancel.Token.IsCancellationRequested && Model.FromDate < DateTime.Now)
            {
                Log.Trace($"{Model.Provider} download {Model.Resolution} {Model.FromDate:d}");
                try
                {
                    _appDomain = _appDomainService.CreateAppDomain();
                    Toolbox toolbox = _appDomainService.CreateInstance<Toolbox>(_appDomain);
                    _cancel = new CancellationTokenSource();
                    await Task.Run(() => Model.Logs += toolbox.Run(Model), _cancel.Token);
                    AppDomain.Unload(_appDomain);
                }
                catch (AppDomainUnloadedException)
                {
                    Log.Trace($"Market {Model.Name} canceled by user");
                }
                catch (Exception ex)
                {
                    Log.Trace($"{ex.GetType()}: {ex.Message}");
                }

                DataFromModel();

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

            Log.Trace($"{Model.Provider} download complete");
            _cancel = null;
            Enabled = false;
        }

        private void StopTask()
        {
            if (_cancel != null)
            {
                _cancel.Cancel();
            }

            if (_appDomain != null)
            {
                AppDomain.Unload(_appDomain);
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
