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
using Algoloop.ViewModel.Internal.Provider;
using Capnode.Wpf.DataGrid;
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
using static Algoloop.ViewModel.SymbolViewModel;
using System.Threading;
using QuantConnect.Statistics;
using QuantConnect.Data.Market;
using Algoloop.ViewModel.Properties;
using CommunityToolkit.Mvvm.Input;

namespace Algoloop.ViewModel
{
    public class MarketViewModel : ViewModelBase, ITreeViewModel
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
        private AccountViewModel _selectedAccount;
        private bool _setUpdate;

        public MarketViewModel(MarketsViewModel marketsViewModel, ProviderModel marketModel, SettingModel settings)
        {
            _parent = marketsViewModel ?? throw new ArgumentNullException(nameof(marketsViewModel));
            Model = marketModel;
            _settings = settings;

            ActiveCommand = new RelayCommand(
                async () => await DoActiveCommand(Model.Active).ConfigureAwait(false),
                () => !IsBusy);
            StartCommand = new RelayCommand(
                async () => await DoStartCommand().ConfigureAwait(false),
                () => !IsBusy && !Active);
            StopCommand = new RelayCommand(
                () => DoStopCommand(),
                () => !IsBusy && Active);
            CheckAllCommand = new RelayCommand<IList>(
                m => DoCheckAll(m),
                m => !IsBusy && !Active && SelectedSymbol != null);
            AddSymbolCommand = new RelayCommand(() => DoAddSymbol(), () => !IsBusy);
            RemoveSymbolsCommand = new RelayCommand<IList>(
                m => DoDeleteSymbols(m),
                m => !IsBusy && SelectedSymbol != null);
            ImportSymbolsCommand = new RelayCommand(
                () => DoImportSymbols(),
                () => !IsBusy);
            ExportSymbolsCommand = new RelayCommand<IList>(
                m => DoExportSymbols(m),
                m => !IsBusy && !Active && SelectedSymbol != null);
            AddToSymbolListCommand = new RelayCommand<IList>(
                m => DoAddToSymbolList(m),
                m => !IsBusy && SelectedSymbol != null);
            DeleteCommand = new RelayCommand(
                () => _parent?.DoDeleteMarket(this),
                () => !IsBusy && !Active);
            NewListCommand = new RelayCommand(
                () => DoNewList(),
                () => !IsBusy && !Active);
            ImportListCommand = new RelayCommand(
                () => DoImportList(),
                () => !IsBusy && !Active);

            Model.ModelChanged += DataFromModel;

            DataFromModel();
//            DoActiveCommand(Active).Wait();
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
        public RelayCommand<IList> CheckAllCommand { get; }
        public RelayCommand AddSymbolCommand { get; }
        public RelayCommand DownloadSymbolListCommand { get; }
        public RelayCommand<IList> RemoveSymbolsCommand { get; }
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
        public SyncObservableCollection<SymbolViewModel> ActiveSymbols { get; } = new SyncObservableCollection<SymbolViewModel>();
        public SyncObservableCollection<ListViewModel> Lists { get; } = new SyncObservableCollection<ListViewModel>();
        public ObservableCollection<AccountViewModel> Accounts { get; } = new ObservableCollection<AccountViewModel>();
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
                    Messenger.Send(new NotificationMessage(message), 0);
                }
            }
        }

        public bool Active
        {
            get => Model.Active;
            set
            {
                Model.Active = value;
                OnPropertyChanged();
                RaiseCommands();
            }
        }

        public ProviderModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        public Resolution SelectedResolution
        {
            get => _selectedResolution;
            set => SetProperty(ref _selectedResolution, value);
        }

        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public AccountViewModel SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                SetProperty(ref _selectedAccount, value);
                ReloadAccount();
            }
        }

        public ReportPeriod SelectedReportPeriod
        {
            get => _selectedReportPeriod;
            set => SetProperty(ref _selectedReportPeriod, value);
        }

        public SymbolViewModel SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                SetProperty(ref _selectedSymbol, value);
                RaiseCommands();
            }
        }

        public bool CheckAll
        {
            get => _checkAll;
            set => SetProperty(ref _checkAll, value);
        }

        public ObservableCollection<DataGridColumn> SymbolColumns
        {
            get => _symbolColumns;
            set => SetProperty(ref _symbolColumns, value);
        }

        public void Refresh()
        {
            _setUpdate = true;
            Model.Refresh();
            foreach (ListViewModel list in Lists)
            {
                list.Refresh();
            }

            // Sync Active Symbols to ActiveSymbols
            IEnumerable<SymbolViewModel> activeSymbols = Symbols.Where(m => m.Active);
            Collection.SmartCopy(activeSymbols, ActiveSymbols);
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
            UpdateColumns();
            SymbolsFromModel();

            // Update Lists
            Lists.Clear();
            foreach (ListModel listModel in Model.Lists.OrderBy(m => m.Name))
            {
                var listViewModel = new ListViewModel(this, listModel);
                Lists.Add(listViewModel);
            }

            Accounts.Clear();
            foreach (AccountModel account in Model.Accounts)
            {
                var vm = new AccountViewModel(account);
                Accounts.Add(vm);
                if (account.Id == Model.DefaultAccountId)
                {
                    SelectedAccount = vm;
                }
            }

            ReloadAccount();
        }

        private void ReloadAccount()
        {
            if (SelectedAccount == null) return;
            Model.DefaultAccountId = SelectedAccount.Model.Id;

            Balances.Clear();
            Orders.Clear();
            Positions.Clear();

            foreach (BalanceModel balance in SelectedAccount.Model.Balances)
            {
                var vm = new BalanceViewModel(balance);
                Balances.Add(vm);
            }

            foreach (OrderModel order in SelectedAccount.Model.Orders)
            {
                var vm = new OrderViewModel(order);
                Orders.Add(vm);
            }

            foreach (PositionModel position in SelectedAccount.Model.Positions)
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

        private async Task DoActiveCommand(bool value)
        {
            if (value)
            {
                // No IsBusy
                await StartMarketAsync().ConfigureAwait(false);
            }
            else
            {
                Model.Active = false;
            }
        }

        private void RaiseCommands()
        {
            ActiveCommand.NotifyCanExecuteChanged();
            StartCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
            CheckAllCommand.NotifyCanExecuteChanged();
            AddSymbolCommand.NotifyCanExecuteChanged();
            RemoveSymbolsCommand.NotifyCanExecuteChanged();
            ImportSymbolsCommand.NotifyCanExecuteChanged();
            ExportSymbolsCommand.NotifyCanExecuteChanged();
            AddToSymbolListCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
            NewListCommand.NotifyCanExecuteChanged();
            ImportListCommand.NotifyCanExecuteChanged();
        }

        private async Task StartMarketAsync()
        {
            try
            {
                DataToModel();
                _provider = ProviderFactory.CreateProvider(Model.Provider);
                if (_provider == null) throw new ApplicationException(
                    $"Can not create provider {Model.Provider}");
                Messenger.Send(new NotificationMessage(
                    string.Format(Resources.MarketStarted, Model.Name)),
                    0);
                await Task.Run(() => MarketLoop(Model)).ConfigureAwait(false);
                IList<string> symbols = Model.Symbols.
                    Where(x => x.Active).Select(m => m.Id).ToList();
                if (symbols.Any())
                {
                    Messenger.Send(new NotificationMessage(
                        string.Format(Resources.MarketCompleted, Model.Name)),
                        0);
                }
                else
                {
                    Messenger.Send(new NotificationMessage(
                        string.Format(Resources.MarketNoSymbol, Model.Name)),
                        0);
                }
            }
            catch (ApplicationException ex)
            {
                Model.Active = false;
                Messenger.Send(new NotificationMessage(
                    string.Format(Resources.MarketException, Model.Name, ex.Message)),
                    0);
            }
            catch (Exception ex)
            {
                Model.Active = false;
                Log.Error(ex);
                Messenger.Send(new NotificationMessage(
                    $"{ex.GetType()}: {ex.Message}"),
                    0);
            }
            finally
            {
                _provider?.Dispose();
                _provider = null;
                UiThread(() =>
                {
                    ProviderModel model = Model;
                    Model = null;
                    Model = model;
                    DataFromModel();
                });
            }
        }

        private void MarketLoop(ProviderModel model)
        {
            _provider.Login(model);
            _setUpdate = true;
            while (model.Active)
            {
//                Log.Trace("MainLoop", true);

                if (_setUpdate)
                {
                    _provider.SetUpdate(model, OnUpdate);
                    _setUpdate = false;
                }
                else
                {
                    _provider.GetUpdate(model, OnUpdate);
                }

                // Update settings page
                UiThread(() =>
                {
                    Model = null;
                    Model = model;
                });
                Thread.Sleep(1000);
            }

            _provider.Logout();
        }

        private void OnUpdate(object data)
        {
            if (data is IEnumerable<AccountModel> accounts)
            {
                Model.UpdateAccounts(accounts);
                UiThread(() => UpdateAccounts(accounts));
                return;
            }

            if (data is IEnumerable<SymbolModel> symbols)
            {
                UiThread(() => UpdateSymbols(symbols));
                Symbols.Sort();
                return;
            }

            if (data is QuoteBar quote)
            {
                UpdateQuote(quote);
                return;
            }

            if (data is IEnumerable<QuoteBar> quotes)
            {
                foreach (QuoteBar quoteBar in quotes)
                {
                    UpdateQuote(quoteBar);
                }
                return;
            }

            if (data is TradeBar trade)
            {
                return;
            }

            Log.Trace("Not processed");
        }

        private void UpdateAccounts(IEnumerable<AccountModel> accounts)
        {
            foreach (AccountModel account in accounts)
            {
                if (account.Id == Model.DefaultAccountId || accounts.Count() == 1)
                {
                    UpdateBalances(account.Balances);
                    UpdatePositions(account.Positions);
                    UpdateOrders(account.Orders);
                }
            }
        }

        private void UpdateSymbols(IEnumerable<SymbolModel> symbols)
        {
            Debug.WriteLine($"UpdateSymbols count={symbols.Count()}");
            foreach (SymbolModel symbol in symbols)
            {
                SymbolViewModel vm = Symbols.FirstOrDefault(m => m.Model.Id.Equals(symbol.Id));
                if (vm == default)
                {
                    vm = new (this, symbol);
                    Symbols.Add(vm);
                    ExDataGridColumns.AddPropertyColumns(
                        SymbolColumns, symbol.Properties, "Model.Properties", false, true);
                }
                else
                {
                    vm.Update(symbol);
                }
            }
        }

        private void UpdateQuote(QuoteBar quote)
        {
            SymbolViewModel symbolVm = Symbols.FirstOrDefault(
                m => m.Model.Name.Equals(quote.Symbol.ID.Symbol,
                StringComparison.OrdinalIgnoreCase));

            SymbolModel symbol;
            if (symbolVm != null)
            {
                symbol = symbolVm.Model;
                symbolVm.Ask = quote.Ask.Close;
                symbolVm.Bid = quote.Bid.Close;

            }
        }

        private void UpdateBalances(Collection<BalanceModel> balances)
        {
            Log.Trace("UpdateBalances");
            if (balances.Count == Balances.Count)
            {
                IEnumerator<BalanceViewModel> iBalance = Balances.GetEnumerator();
                foreach (BalanceModel balance in balances)
                {
                    if (iBalance.MoveNext())
                    {
                        iBalance.Current.Update(balance);
                    }
                }
            }
            else
            {
                Balances.Clear();
                foreach (BalanceModel balance in balances)
                {
                    var vm = new BalanceViewModel(balance);
                    Balances.Add(vm);
                }
            }
        }

        private void UpdatePositions(Collection<PositionModel> positions)
        {
            Log.Trace("UpdatePositions");
            if (positions.Count == Positions.Count)
            {
                IEnumerator<PositionViewModel> iPosition = Positions.GetEnumerator();
                foreach (PositionModel position in positions)
                {
                    if (iPosition.MoveNext())
                    {
                        iPosition.Current.Update(position);
                    }
                }
            }
            else
            {
                Positions.Clear();
                foreach (PositionModel position in positions)
                {
                    var vm = new PositionViewModel(position);
                    Positions.Add(vm);
                }
            }
        }

        private void UpdateOrders(Collection<OrderModel> orders)
        {
            Log.Trace("UpdateOrders");
            if (orders.Count == Balances.Count)
            {
                IEnumerator<OrderViewModel> iOrder = Orders.GetEnumerator();
                foreach (OrderModel order in orders)
                {
                    if (iOrder.MoveNext())
                    {
                        iOrder.Current.Update(order);
                    }
                }
            }
            else
            {
                Orders.Clear();
                foreach (OrderModel order in orders)
                {
                    var vm = new OrderViewModel(order);
                    Orders.Add(vm);
                }
            }
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
                Log.Error(ex, $"Failed reading {openFileDialog.FileName}\n", true);
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
                Log.Error(ex, $"Failed writing {saveFileDialog.FileName}\n", true);
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
                Log.Error(ex, $"Failed reading {openFileDialog.FileName}\n", true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateColumns()
        {
            Active = Model.Active;

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
        }

        private void SymbolsFromModel()
        {
            Symbols.Clear();
            ActiveSymbols.Clear();
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                SymbolViewModel symbolViewModel = new (this, symbolModel);
                Symbols.Add(symbolViewModel);
                if (symbolViewModel.Active)
                {
                    ActiveSymbols.Add(symbolViewModel);
                }

                ExDataGridColumns.AddPropertyColumns(SymbolColumns, symbolModel.Properties, "Model.Properties", false, true);
            }

            Symbols.Sort();
        }
    }
}
