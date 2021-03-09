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
using Algoloop.Wpf.Properties;
using Algoloop.Wpf.Provider;
using Algoloop.Wpf.ViewSupport;
using Capnode.Wpf.DataGrid;
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
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics.Contracts;
using static Algoloop.Wpf.ViewModel.SymbolViewModel;
using System.Threading;
using QuantConnect.Statistics;
using QuantConnect.Data.Market;

namespace Algoloop.Wpf.ViewModel
{
    public class MarketViewModel : ViewModel, ITreeViewModel
    {
        private readonly MarketsViewModel _parent;
        private readonly SettingModel _settings;
        private ProviderModel _model;
        private SymbolViewModel _selectedSymbol;
        private ObservableCollection<DataGridColumn> _symbolColumns = new();
        private bool _checkAll;
        private IList _selectedItems;
        private IProvider _provider;
        private DateTime _date = DateTime.Today;
        private Resolution _selectedResolution = Resolution.Daily;
        private static ReportPeriod _selectedReportPeriod;
        private AccountModel _selectedAccount;

        public MarketViewModel(MarketsViewModel marketsViewModel, ProviderModel marketModel, SettingModel settings)
        {
            _parent = marketsViewModel ?? throw new ArgumentNullException(nameof(marketsViewModel));
            Model = marketModel;
            _settings = settings;

            AccountChangedCommand = new RelayCommand<AccountModel>((m) => DoAccountChanged(m), !IsBusy);
            ActiveCommand = new RelayCommand(async () => await DoActiveCommand(Model.Active).ConfigureAwait(false), !IsBusy);
            StartCommand = new RelayCommand(async () => await DoStartCommand().ConfigureAwait(false), () => !IsBusy && !Active);
            StopCommand = new RelayCommand(() => DoStopCommand(), () => !IsBusy && Active);
            CheckAllCommand = new RelayCommand<IList>(m => DoCheckAll(m), m => !IsBusy && !Active && SelectedSymbol != null);
            AddSymbolCommand = new RelayCommand(() => DoAddSymbol(), () => !IsBusy);
            DeleteSymbolsCommand = new RelayCommand<IList>(m => DoDeleteSymbols(m), m => !IsBusy && !Active && SelectedSymbol != null);
            ImportSymbolsCommand = new RelayCommand(() => DoImportSymbols(), !IsBusy);
            ExportSymbolsCommand = new RelayCommand<IList>(m => DoExportSymbols(m), m => !IsBusy && !Active && SelectedSymbol != null);
            AddToSymbolListCommand = new RelayCommand<IList>(m => DoAddToSymbolList(m), m => !IsBusy && !Active && SelectedSymbol != null);
            DeleteCommand = new RelayCommand(() => _parent?.DoDeleteMarket(this), () => !IsBusy && !Active);
            NewListCommand = new RelayCommand(() => DoNewList(), () => !IsBusy && !Active);
            ImportListCommand = new RelayCommand(() => DoImportList(), () => !IsBusy && !Active);

            Model.ModelChanged += DataFromModel;

            DataFromModel();
            DoActiveCommand(Active).Wait();
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public bool IsBusy
        {
            get => _parent.IsBusy;
            set
            {
                _parent.IsBusy = value;
                RaiseCommands();
            }
        }

        public ITreeViewModel SelectedItem
        {
            get => _parent.SelectedItem;
            set => _parent.SelectedItem = value;
        }

        public RelayCommand<IList> SymbolSelectionChangedCommand { get; }
        public RelayCommand<AccountModel> AccountChangedCommand { get; }
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

        public IEnumerable<Resolution> ResolutionList { get; } = new[] { Resolution.Daily, Resolution.Hour, Resolution.Minute, Resolution.Second, Resolution.Tick };
        public IEnumerable<ReportPeriod> ReportPeriodList { get; } = new[] { ReportPeriod.Year, ReportPeriod.R12, ReportPeriod.Quarter };
        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();
        public SyncObservableCollection<ListViewModel> Lists { get; } = new SyncObservableCollection<ListViewModel>();
        public ObservableCollection<AccountModel> Accounts { get; } = new ObservableCollection<AccountModel>();
        public SyncObservableCollection<BalanceViewModel> Balances { get; } = new SyncObservableCollection<BalanceViewModel>();
        public SyncObservableCollection<OrderViewModel> Orders { get; } = new SyncObservableCollection<OrderViewModel>();
        public SyncObservableCollection<PositionViewModel> Positions { get; } = new SyncObservableCollection<PositionViewModel>();
        public SyncObservableCollection<Trade> ClosedTrades { get; } = new SyncObservableCollection<Trade>();

        public string DataFolder => _settings.DataFolder;

        public IList SelectedItems
        {
            get { return _selectedItems; }
            set
            {
                Contract.Requires(value != null);
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
                RaiseCommands();
            }
        }

        public ProviderModel Model
        {
            get => _model;
            set => Set(ref _model, value);
        }

        public Resolution SelectedResolution
        {
            get => _selectedResolution;
            set => Set(ref _selectedResolution, value);
        }

        public DateTime Date
        {
            get => _date;
            set => Set(ref _date, value);
        }

        public ReportPeriod SelectedReportPeriod
        {
            get => _selectedReportPeriod;
            set => Set(ref _selectedReportPeriod, value);
        }

        public SymbolViewModel SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                Set(ref _selectedSymbol, value);
                RaiseCommands();
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

        public AccountModel SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (value == default) return;
                if (value.Equals(_selectedAccount)) return;
                Set(ref _selectedAccount, value);
            }
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

            Balances.Clear();
            Orders.Clear();
            Positions.Clear();

            // Find selected account
            AccountModel account = Model.Accounts.FirstOrDefault(m => m.Id.Equals(Model.DefaultAccountId));
            if (account == default)
            {
                account = Model.Accounts.ElementAtOrDefault(0);
                if (account == default) return;
            }

            SelectedAccount = account;
            SmartCopy(Model.Accounts, Accounts);

            foreach (BalanceModel balance in account.Balances)
            {
                var vm = new BalanceViewModel(balance);
                Balances.Add(vm);
            }

            foreach (OrderModel order in account.Orders)
            {
                var vm = new OrderViewModel(order);
                Orders.Add(vm);
            }

            foreach (PositionModel position in account.Positions)
            {
                var vm = new PositionViewModel(position);
                Positions.Add(vm);
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

        internal void StopMarket()
        {
            if (Active)
            {
                Active = false;
                _provider?.Logout();
            }
        }

        private async Task DoActiveCommand(bool value)
        {
            if (value)
            {
                // No IsBusy
                await StartMarketAsync().ConfigureAwait(false);
            }
            else
            {
                try
                {
                    IsBusy = true;
                    _provider?.Logout();
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private void RaiseCommands()
        {
            AccountChangedCommand.RaiseCanExecuteChanged();
            ActiveCommand.RaiseCanExecuteChanged();
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
            CheckAllCommand.RaiseCanExecuteChanged();
            AddSymbolCommand.RaiseCanExecuteChanged();
            DeleteSymbolsCommand.RaiseCanExecuteChanged();
            ImportSymbolsCommand.RaiseCanExecuteChanged();
            ExportSymbolsCommand.RaiseCanExecuteChanged();
            AddToSymbolListCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            NewListCommand.RaiseCanExecuteChanged();
            ImportListCommand.RaiseCanExecuteChanged();
        }

        private void SmartCopy<T>(Collection<T> src, ObservableCollection<T> dest)
        {
            int count = src.Count;
            if (count == dest.Count)
            {
                bool equals = true;
                for (int i = 0; i < count; i++)
                {
                    var srcItem = src[i];
                    var destItem = dest[i];
                    if (srcItem.Equals(destItem)) continue;
                    equals = false;
                    break;
                }
                if (equals) return;
            }

            dest.Clear();
            foreach (T item in src)
            {
                dest.Add(item);
            }
        }

        private async Task StartMarketAsync()
        {
            DataToModel();
            try
            {
                _provider = ProviderFactory.CreateProvider(Model.Provider, _settings);
                if (_provider == null) throw new ApplicationException($"Can not create provider {Model.Provider}");
                Messenger.Default.Send(new NotificationMessage(string.Format(Resources.MarketStarted, Model.Name)));
                await Task.Run(() => MarketLoop(Model)).ConfigureAwait(false);
                IList<string> symbols = Model.Symbols.Where(x => x.Active).Select(m => m.Id).ToList();
                if (symbols.Any())
                {
                    Messenger.Default.Send(new NotificationMessage(string.Format(Resources.MarketCompleted, Model.Name)));
                }
                else
                {
                    Messenger.Default.Send(new NotificationMessage(string.Format(Resources.MarketNoSymbol, Model.Name)));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Messenger.Default.Send(new NotificationMessage($"{ex.GetType()}: {Model.Name} {ex.Message} "));
                UiThread(() => Active = false);
            }
            finally
            {
                _provider?.Dispose();
                _provider = null;
            }
        }

        private void MarketLoop(ProviderModel model)
        {
            _provider.Login(model);
            while (model.Active)
            {
                IReadOnlyList<AccountModel> accounts = _provider.GetAccounts(model);
                model.UpdateAccounts(accounts);
                IReadOnlyList<SymbolModel> symbols = _provider.GetMarketData(model);
                model.UpdateSymbols(symbols);

                // Update view
                UiThread(() =>
                {
                    Model = null; // Trick to update settings page
                    Model = model;
                    DataFromModel();
                });

                Thread.Sleep(1000);
            }

            _provider.Logout();
        }

        private void OnAccountsUpdate(object data)
        {
        }

        private void OnMarketUpdate(object data)
        {
            if (data is QuoteBar quote)
            {
                Log.Trace($"Quote:{quote}");
                SymbolViewModel symbolVm = Symbols.FirstOrDefault(
                    m => m.Model.Id.Equals(quote.Symbol.ID.Symbol,
                    StringComparison.OrdinalIgnoreCase));
                if (symbolVm == default) return;
                SymbolModel symbol;
                if (symbolVm != null)
                {
                    symbol = symbolVm.Model;
                }
                else
                {
                    symbol = new SymbolModel(quote.Symbol)
                    {
                        Properties = new Dictionary<string, object>
                        {
                            { "Ask", quote.Ask.Close },
                            { "Bid", quote.Bid.Close }
                        }
                    };
                    Model.Symbols.Add(symbol);
                    UiThread(() => Symbols.Add(new SymbolViewModel(this, symbol)));
                }
            }
            else if (data is TradeBar trade)
            {
                Log.Trace($"Trade:{trade}");
            }
        }

        //private void UpdateOrder(IProvider provider)
        //{
        //    IReadOnlyList<Order> orders = provider.GetOpenOrders();
        //    foreach (Order order in orders)
        //    {
        //        bool update = false;
        //        foreach (OrderViewModel vm in Orders)
        //        {
        //            if (order.Id == vm.Id)
        //            {
        //                vm.Update(order);
        //                update = true;
        //                break;
        //            }
        //        }

        //        if (!update)
        //        {
        //            Orders.Add(new OrderViewModel(order));
        //        }
        //    }
        //}

        //private void UpdatePosition(IProvider provider)
        //{
        //    IReadOnlyList<Holding> holdings = provider.GetAccountHoldings();
        //    foreach (Holding holding in holdings)
        //    {
        //        bool update = false;
        //        foreach (PositionViewModel vm in Positions)
        //        {
        //            if (holding.Symbol.Value == vm.Symbol)
        //            {
        //                vm.Update(holding);
        //                update = true;
        //                break;
        //            }
        //        }

        //        if (!update)
        //        {
        //            Positions.Add(new PositionViewModel(holding));
        //        }
        //    }

        //    PositionViewModel[] vms = new PositionViewModel[Positions.Count];
        //    Positions.CopyTo(vms, 0);
        //    foreach (PositionViewModel vm in vms)
        //    {
        //        Holding holding = holdings.FirstOrDefault(m => m.Symbol.Value == vm.Symbol);
        //        if (holding == null || holding.Symbol == null)
        //        {
        //            Positions.Remove(vm);
        //        }
        //    }
        //}

        //private void UpdateClosedTrades(IProvider provider)
        //{
        //    ClosedTrades.Clear();
        //    IReadOnlyList<Trade> trades = provider.GetClosedTrades();
        //    foreach (Trade trade in trades)
        //    {
        //        ClosedTrades.Add(trade);
        //    }
        //}

        //private void UpdateBalance(IProvider provider)
        //{
        //    IReadOnlyList<CashAmount> cashAmounts = provider.GetCashBalance();
        //    foreach (CashAmount cashAmount in cashAmounts)
        //    {
        //        bool update = false;
        //        foreach (BalanceViewModel vm in Balances)
        //        {
        //            if (cashAmount.Currency == vm.Model.Currency)
        //            {
        //                vm.Update(cashAmount);
        //                update = true;
        //                break;
        //            }
        //        }

        //        if (!update)
        //        {
        //            var balance = new BalanceModel
        //            {
        //                Currency = cashAmount.Currency,
        //                Cash = cashAmount.Amount
        //            };
        //            Balances.Add(new BalanceViewModel(balance));
        //        }
        //    }

        //    BalanceViewModel[] vms = new BalanceViewModel[Balances.Count];
        //    Balances.CopyTo(vms, 0);
        //    foreach (BalanceViewModel vm in vms)
        //    {
        //        CashAmount cashAmount = cashAmounts.FirstOrDefault(m => m.Currency == vm.Model.Currency);
        //        if (cashAmount == null || cashAmount.Currency == null)
        //        {
        //            Balances.Remove(vm);
        //        }
        //    }
        //}


        private void DoAccountChanged(AccountModel account)
        {
            if (account == default) return;
            Model.DefaultAccountId = account.Id;
        }

        internal async Task DoStartCommand()
        {
            // No IsBusy
            Active = true;
            await StartMarketAsync().ConfigureAwait(false);
        }

        internal void DoStopCommand()
        {
            try
            {
                IsBusy = true;
                StopMarket();
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
            OpenFileDialog openFileDialog = new()
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
                    using var r = new StreamReader(fileName);
                    while (!r.EndOfStream)
                    {
                        string line = r.ReadLine();
                        foreach (string name in line.Split(',').Where(m => !string.IsNullOrWhiteSpace(m)))
                        {
                            SymbolModel symbol = Model.Symbols.FirstOrDefault(m => m.Id.Equals(name, StringComparison.OrdinalIgnoreCase));
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

            var saveFileDialog = new SaveFileDialog
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
                    file.WriteLine(symbol.Model.Id);
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
            var openFileDialog = new OpenFileDialog
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
                    using var r = new StreamReader(fileName);
                    while (!r.EndOfStream)
                    {
                        string line = r.ReadLine();
                        foreach (string name in line.Split(',').Where(m => !string.IsNullOrWhiteSpace(m)))
                        {
                            var symbol = Model.Symbols.FirstOrDefault(m =>
                                m.Id.Equals(name, StringComparison.OrdinalIgnoreCase) &&
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
                Header = "Name",
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

            Symbols.Sort();
        }

        private void DbUpgrade(SymbolModel symbol)
        {
            var basedir = new DirectoryInfo(DataFolder);
            foreach (DirectoryInfo securityDir in basedir.GetDirectories())
            {
                foreach (DirectoryInfo marketDir in securityDir.GetDirectories())
                {
                    foreach (DirectoryInfo resolutionDir in marketDir.GetDirectories())
                    {
                        if (resolutionDir.GetDirectories(symbol.Id).Any()
                            || resolutionDir.GetFiles(symbol.Id + ".zip").Any())
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
    }
}
