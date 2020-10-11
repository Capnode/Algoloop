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
using Algoloop.Lean;
using Algoloop.Model;
using Algoloop.Properties;
using Algoloop.Provider;
using Algoloop.ViewSupport;
using Capnode.Wpf.DataGrid;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using QuantConnect;
using QuantConnect.Logging;
using QuantConnect.Securities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace Algoloop.ViewModel
{
    public class MarketViewModel : ViewModelBase, ITreeViewModel, IDisposable
    {
        private bool _isDisposed = false; // To detect redundant calls
        private readonly MarketsViewModel _parent;
        private readonly SettingModel _settings;
        private CancellationTokenSource _cancel;
        private MarketModel _model;
        private Isolated<ProviderFactory> _factory;
        private SymbolViewModel _selectedSymbol;
        private ObservableCollection<DataGridColumn> _symbolColumns = new ObservableCollection<DataGridColumn>();
        private bool _checkAll;
        private IList _selectedItems;

        public MarketViewModel(MarketsViewModel marketsViewModel, MarketModel marketModel, SettingModel settings)
        {
            _parent = marketsViewModel ?? throw new ArgumentNullException(nameof(marketsViewModel));
            Model = marketModel;
            _settings = settings;

            CheckAllCommand = new RelayCommand<IList>(m => DoCheckAll(m), m => !IsBusy && !Active && SelectedSymbol != null);
            AddSymbolCommand = new RelayCommand(() => DoAddSymbol(), () => !IsBusy);
            DeleteSymbolsCommand = new RelayCommand<IList>(m => DoDeleteSymbols(m), m => !IsBusy && !Active && SelectedSymbol != null);
            ImportSymbolsCommand = new RelayCommand(() => DoImportSymbols(), !IsBusy);
            ExportSymbolsCommand = new RelayCommand<IList>(m => DoExportSymbols(m), m => !IsBusy && !Active && SelectedSymbol != null);
            AddToSymbolListCommand = new RelayCommand<IList>(m => DoAddToSymbolList(m), m => !IsBusy && !Active && SelectedSymbol != null);
            DeleteCommand = new RelayCommand(() => _parent?.DoDeleteMarket(this), () => !IsBusy && !Active);
            NewListCommand = new RelayCommand(() => DoNewList(), () => !IsBusy && !Active);
            ImportListCommand = new RelayCommand(() => DoImportList(), () => !IsBusy && !Active);
            ActiveCommand = new RelayCommand(() => DoActiveCommand(Model.Active), !IsBusy);
            StartCommand = new RelayCommand(() => DoStartCommand(), () => !IsBusy && !Active);
            StopCommand = new RelayCommand(() => DoStopCommand(), () => !IsBusy && Active);

            Model.ModelChanged += DataFromModel;

            DataFromModel();
            DoActiveCommand(Active);
        }

        public bool IsBusy
        {
            get => _parent.IsBusy;
            set => _parent.IsBusy = value;
        }

        public ITreeViewModel SelectedItem
        {
            get => _parent.SelectedItem;
            set => _parent.SelectedItem = value;
        }

        public RelayCommand<IList> SymbolSelectionChangedCommand { get; }
        public RelayCommand<IList> CheckAllCommand { get; }
        public RelayCommand AddSymbolCommand { get; }
        public RelayCommand DownloadSymbolListCommand { get; }
        public RelayCommand<IList> DeleteSymbolsCommand { get; }
        public RelayCommand ImportSymbolsCommand { get; }
        public RelayCommand<IList> ExportSymbolsCommand { get; }
        public RelayCommand<IList> AddToSymbolListCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand NewListCommand { get; }
        public RelayCommand ImportListCommand { get; }
        public RelayCommand ActiveCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();
        public SyncObservableCollection<ListViewModel> Lists { get; } = new SyncObservableCollection<ListViewModel>();
        public string DataFolder => _settings.DataFolder;

        public IList SelectedItems
        {
            get { return _selectedItems; }
            set
            {
                _selectedItems = value;
                if (_selectedItems?.Count > 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture, Resources.SelectedCount, _selectedItems.Count);
                    Messenger.Default.Send(new NotificationMessage(message));
                }
            }
        }

        public bool Active
        {
            get => Model.Active;
            set
            {
                Model.Active = value;
                RaisePropertyChanged(() => Active);

                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                DeleteSymbolsCommand.RaiseCanExecuteChanged();
                ExportSymbolsCommand.RaiseCanExecuteChanged();
                AddToSymbolListCommand.RaiseCanExecuteChanged();
                CheckAllCommand.RaiseCanExecuteChanged();
            }
        }

        public MarketModel Model
        {
            get => _model;
            set => Set(ref _model, value);
        }

        public SymbolViewModel SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                Set(ref _selectedSymbol, value);
                DeleteSymbolsCommand.RaiseCanExecuteChanged();
                ExportSymbolsCommand.RaiseCanExecuteChanged();
                AddToSymbolListCommand.RaiseCanExecuteChanged();
                CheckAllCommand.RaiseCanExecuteChanged();
            }
        }

        public bool CheckAll
        {
            get => _checkAll;
            set => Set(ref _checkAll, value);
        }

        public ObservableCollection<DataGridColumn> SymbolColumns
        {
            get => _symbolColumns;
            set => Set(ref _symbolColumns, value);
        }

        public void Refresh()
        {
            Model.Refresh();
            foreach (ListViewModel list in Lists)
            {
                list.Refresh();
            }
        }

        internal void DataToModel()
        {
            Model.Symbols.Clear();
            foreach (SymbolViewModel symbol in Symbols)
            {
                Model.Symbols.Add(symbol.Model);
            }

            Model.Lists.Clear();
            foreach (ListViewModel list in Lists)
            {
                Model.Lists.Add(list.Model);
                list.DataToModel();
            }
        }

        internal void DataFromModel()
        {
            Active = Model.Active;
            UpdateSymbolsAndColumns();

            // Update Lists
            Lists.Clear();
            foreach (ListModel listModel in Model.Lists.OrderBy(m => m.Name))
            {
                var listViewModel = new ListViewModel(this, listModel);
                Lists.Add(listViewModel);
            }
        }

        internal void DeleteList(ListViewModel symbol)
        {
            Lists.Remove(symbol);
            DataToModel();
        }


        internal void DeleteSymbol(SymbolViewModel symbol)
        {
            Symbols.Remove(symbol);
            DataToModel();
        }

        private async Task DownloadAsync()
        {
            DataToModel();
            MarketModel market = Model;
            _cancel = new CancellationTokenSource();
            while (!_cancel.Token.IsCancellationRequested && market.Active)
            {
                Log.Trace($"{market.Provider} download {market.Resolution} {market.LastDate:d}");
                using var logger = new HostDomainLogger();
                try
                {
                    _factory = new Isolated<ProviderFactory>();
                    _cancel = new CancellationTokenSource();
                    await Task
                        .Run(() => market = _factory.Value.Download(market, _settings, logger), _cancel.Token)
                        .ConfigureAwait(true);
                    _factory.Dispose();
                    _factory = null;
                }
                catch (AppDomainUnloadedException)
                {
                    Log.Trace($"Market {market.Name} canceled by user");
                    _factory = null;
                    market.Active = false;
                }
                catch (Exception ex)
                {
                    Log.Trace($"{ex.GetType()}: {ex.Message}");
                    _factory.Dispose();
                    _factory = null;
                    market.Active = false;
                }

                if (logger.IsError)
                {
                    Log.Trace($"{Model.Provider} download failed");
                }

                // Update view
                Model = null;
                Model = market;
                DataFromModel();
            }

            _cancel = null;
            IList<string> symbols = market.Symbols.Where(x => x.Active).Select(m => m.Name).ToList();
            Messenger.Default.Send(new NotificationMessage(
                symbols.Any() ? Resources.DownloadComplete : Resources.NoSymbolSelected));
        }

        private void StopTask()
        {
            if (_cancel != null)
            {
                _cancel.Cancel();
            }

            if (_factory != null)
            {
                _factory.Dispose();
            }
        }

        private async void DoActiveCommand(bool value)
        {
            // No IsBusy
            if (value)
            {
                await DownloadAsync().ConfigureAwait(true);
            }
            else
            {
                StopTask();
            }
        }

        private async void DoStartCommand()
        {
            // No IsBusy
            Active = true;
            await DownloadAsync().ConfigureAwait(true);
        }

        private void DoStopCommand()
        {
            try
            {
                IsBusy = true;
                StopTask();
                Active = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoAddSymbol()
        {
            try
            {
                IsBusy = true;
                var symbol = new SymbolViewModel(this, new SymbolModel("symbol", string.Empty, SecurityType.Base));
                Symbols.Add(symbol);
                DataToModel();
                Lists.ToList().ForEach(m => m.Refresh());
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoCheckAll(IList items)
        {
            List<SymbolViewModel> symbols = items.Cast<SymbolViewModel>()?.ToList();
            Debug.Assert(symbols != null);
            if (symbols.Count == 0)
                return;

            try
            {
                IsBusy = true;
                symbols.ForEach(m => m.Active = CheckAll);

                // Update lists
                foreach (ListViewModel list in Lists)
                {
                    list.Refresh();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoDeleteSymbols(IList symbols)
        {
            Debug.Assert(symbols != null);
            if (Symbols.Count == 0 || symbols.Count == 0)
                return;

            try
            {
                IsBusy = true;
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
            finally
            {
                IsBusy = false;
            }
        }

        private void DoNewList()
        {
            Lists.Add(new ListViewModel(this, new ListModel()));
            DataToModel();
        }

        private void DoImportSymbols()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                Multiselect = false,
                Filter = "symbol file (*.csv)|*.csv|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == false)
                return;

            try
            {
                IsBusy = true;
                foreach (string fileName in openFileDialog.FileNames)
                {
                    using StreamReader r = new StreamReader(fileName);
                    while (!r.EndOfStream)
                    {
                        string line = r.ReadLine();
                        foreach (string name in line.Split(',').Where(m => !string.IsNullOrWhiteSpace(m)))
                        {
                            SymbolModel symbol = Model.Symbols.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                            if (symbol != null)
                            {
                                symbol.Active = true;
                            }
                            else
                            {
                                symbol = new SymbolModel(name, string.Empty, SecurityType.Base);
                                Model.Symbols.Add(symbol);
                            }
                        }
                    }
                }

                Lists.ToList().ForEach(m => m.Refresh());
                DataFromModel();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed reading {openFileDialog.FileName}\n");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoExportSymbols(IList symbols)
        {
            Debug.Assert(symbols != null);
            if (symbols.Count == 0)
                return;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = "symbol file (*.csv)|*.csv|All files (*.*)|*.*"
            };
            if (saveFileDialog.ShowDialog() == false)
                return;

            try
            {
                IsBusy = true;
                string fileName = saveFileDialog.FileName;
                using StreamWriter file = File.CreateText(fileName);
                foreach (SymbolViewModel symbol in symbols)
                {
                    file.WriteLine(symbol.Model.Name);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed writing {saveFileDialog.FileName}\n");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoAddToSymbolList(IList items)
        {
            List<SymbolViewModel> symbols = items.Cast<SymbolViewModel>()?.ToList();
            Debug.Assert(symbols != null);
            if (symbols.Count == 0)
                return;

            try
            {
                IsBusy = true;
                var list = new ListViewModel(this, new ListModel());
                list.AddSymbols(symbols.Where(m => m.Active));
                Lists.Add(list);
                DataToModel();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoImportList()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                Multiselect = true,
                Filter = "symbol file (*.csv)|*.csv|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }

            try
            {
                IsBusy = true;
                foreach (string fileName in openFileDialog.FileNames)
                {
                    var model = new ListModel { Name = Path.GetFileNameWithoutExtension(fileName) };
                    Model.Lists.Add(model);
                    using StreamReader r = new StreamReader(fileName);
                    while (!r.EndOfStream)
                    {
                        string line = r.ReadLine();
                        foreach (string name in line.Split(',').Where(m => !string.IsNullOrWhiteSpace(m)))
                        {
                            var symbol = Model.Symbols.FirstOrDefault(m =>
                                m.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                                m.Active);
                            if (symbol != null)
                            {
                                model.Symbols.Add(symbol);
                            }
                        }
                    }
                }

                DataFromModel();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed reading {openFileDialog.FileName}\n");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateSymbolsAndColumns()
        {
            Active = Model.Active;
            Symbols.Clear();

            SymbolColumns.Clear();
            SymbolColumns.Add(new DataGridCheckBoxColumn()
            {
                Header = "Active",
                Binding = new Binding("Active") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
            });
            SymbolColumns.Add(new DataGridTextColumn()
            {
                Header = "Symbol",
                Binding = new Binding("Model.Name") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
            });
            SymbolColumns.Add(new DataGridTextColumn()
            {
                Header = "Market",
                Binding = new Binding("Model.Market") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
            });
            SymbolColumns.Add(new DataGridTextColumn()
            {
                Header = "Security",
                Binding = new Binding("Model.Security") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
            });

            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                // Handle DB upgrade
                if (string.IsNullOrEmpty(symbolModel.Market) || symbolModel.Security == SecurityType.Base)
                {
                    DbUpgrade(symbolModel);
                }

                var symbolViewModel = new SymbolViewModel(this, symbolModel);
                Symbols.Add(symbolViewModel);
                ExDataGridColumns.AddPropertyColumns(SymbolColumns, symbolModel.Properties, "Model.Properties", false, true);
            }
        }

        private void DbUpgrade(SymbolModel symbol)
        {
            DirectoryInfo basedir = new DirectoryInfo(DataFolder);
            foreach (DirectoryInfo securityDir in basedir.GetDirectories())
            {
                foreach (DirectoryInfo marketDir in securityDir.GetDirectories())
                {
                    foreach (DirectoryInfo resolutionDir in marketDir.GetDirectories())
                    {
                        if (resolutionDir.GetDirectories(symbol.Name).Any()
                            || resolutionDir.GetFiles(symbol.Name + ".zip").Any())
                        {
                            if (Enum.TryParse<SecurityType>(securityDir.Name, true, out SecurityType security))
                            {
                                symbol.Security = security;
                                symbol.Market = marketDir.Name;
                                Log.Trace($"DB upgrade symbol {symbol}");
                                return;
                            }
                        }
                    }
                }
            }

            Log.Error($"DB upgrade symbol {symbol} failed!");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _cancel.Dispose();
                    _factory.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}
