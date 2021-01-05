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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Algoloop.Model;
using Algoloop.Wpf.Provider;
using Algoloop.Wpf.Properties;
using Algoloop.Wpf.ViewSupport;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using QuantConnect;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Statistics;
using System.Linq;

namespace Algoloop.Wpf.ViewModel
{
    public class BrokerViewModel : ViewModel, ITreeViewModel, IDisposable
    {
        private bool _isDisposed = false; // To detect redundant calls
        private readonly AccountsViewModel _parent;
        private readonly SettingModel _settings;
        private CancellationTokenSource _cancel;
        private IList _selectedItems;
        private BrokerModel _model;

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
                    _cancel?.Dispose();
                }

                _isDisposed = true;
            }
        }

        public BrokerViewModel(AccountsViewModel accountsViewModel, BrokerModel accountModel, SettingModel settings)
        {
            _parent = accountsViewModel;
            Model = accountModel;
            _settings = settings;

            ActiveCommand = new RelayCommand(() => DoActiveCommand(Model.Active), () => !IsBusy);
            StartCommand = new RelayCommand(() => DoStartCommand(), () => !IsBusy && !Active);
            StopCommand = new RelayCommand(() => DoStopCommand(), () => !IsBusy && Active);
            DeleteCommand = new RelayCommand(() => _parent?.DoDeleteBroker(this), () => !IsBusy && !Active);

            DataFromModel();
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

        public RelayCommand ActiveCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand DeleteCommand { get; }

        public BrokerModel Model
        {
            get => _model;
            set => Set(ref _model, value);
        }

        public SyncObservableCollection<OrderViewModel> Orders { get; } = new SyncObservableCollection<OrderViewModel>();
        public SyncObservableCollection<PositionViewModel> Positions { get; } = new SyncObservableCollection<PositionViewModel>();
        public SyncObservableCollection<Trade> ClosedTrades { get; } = new SyncObservableCollection<Trade>();
        public SyncObservableCollection<BalanceViewModel> Balances { get; } = new SyncObservableCollection<BalanceViewModel>();

        public IList SelectedItems
        {
            get { return _selectedItems; }
            set
            {
                Contract.Requires(value != null);
                _selectedItems = value;
                string message = string.Empty;
                if (_selectedItems?.Count > 0)
                {
                    message = string.Format(CultureInfo.InvariantCulture, Resources.SelectedCount, _selectedItems.Count);
                }

                Messenger.Default.Send(new NotificationMessage(message));
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

        public void Refresh()
        {
            Model.Refresh();
        }

        internal async Task StartAccountAsync()
        {
            Debug.Assert(Active);

            try
            {
                Debug.Assert(_cancel == null);
                _cancel = new CancellationTokenSource();
                await Task.Run(AccountLoop, _cancel.Token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Messenger.Default.Send(new NotificationMessage($"{Model.Name}: {ex.Message}"));
                Log.Error(ex, Model.Name);

            }
            finally
            {
                _cancel?.Dispose();
                _cancel = null;
            }

            // Update view
            UiThread(() =>
            {
                Active = false;
            });
        }

        internal void DataToModel()
        {
            Contract.Requires(Model != null);
        }

        internal void DataFromModel()
        {
        }

        private void RaiseCommands()
        {
            ActiveCommand.RaiseCanExecuteChanged();
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
        }

        private async void DoStartCommand()
        {
            // No IsBusy
            Active = true;
            await StartAccountAsync().ConfigureAwait(false);
        }

        private void DoStopCommand()
        {
            try
            {
                IsBusy = true;
                _cancel?.Cancel();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void DoActiveCommand(bool value)
        {
            if (value)
            {
                await StartAccountAsync().ConfigureAwait(false);
            }
            else
            {
                DoStopCommand();
            }
        }

        private void AccountLoop()
        {
            using IProvider provider = ProviderFactory.CreateProvider(Model.Name, _settings);
            if (provider == null) throw new ApplicationException($"Can not create provider {Model.Provider}");
            IReadOnlyList<AccountModel> accounts = provider.Login(Model, _settings);
            while (!_cancel.IsCancellationRequested)
            {
                UpdateOrder(provider);
                UpdatePosition(provider);
                UpdateBalance(provider);
                UpdateClosedTrades(provider);
                Thread.Sleep(1000);
            }

            provider.Logout();
            Orders.Clear();
            Positions.Clear();
            Balances.Clear();
            ClosedTrades.Clear();
        }

        private void UpdateOrder(IProvider provider)
        {
            IReadOnlyList<Order> orders = provider.GetOpenOrders();
            foreach (Order order in orders)
            {
                bool update = false;
                foreach (OrderViewModel vm in Orders)
                {
                    if (order.Id == vm.Id)
                    {
                        vm.Update(order);
                        update = true;
                        break;
                    }
                }

                if (!update)
                {
                    Orders.Add(new OrderViewModel(order));
                }
            }
        }

        private void UpdatePosition(IProvider provider)
        {
            IReadOnlyList<Holding> holdings = provider.GetAccountHoldings();
            foreach (Holding holding in holdings)
            {
                bool update = false;
                foreach (PositionViewModel vm in Positions)
                {
                    if (holding.Symbol.Value == vm.Symbol)
                    {
                        vm.Update(holding);
                        update = true;
                        break;
                    }
                }

                if (!update)
                {
                    Positions.Add(new PositionViewModel(holding));
                }
            }

            PositionViewModel[] vms = new PositionViewModel[Positions.Count];
            Positions.CopyTo(vms, 0);
            foreach (PositionViewModel vm in vms)
            {
                Holding holding = holdings.FirstOrDefault(m => m.Symbol.Value == vm.Symbol);
                if (holding == null || holding.Symbol == null)
                {
                    Positions.Remove(vm);
                }
            }
        }

        private void UpdateClosedTrades(IProvider provider)
        {
            ClosedTrades.Clear();
            IReadOnlyList<Trade> trades = provider.GetClosedTrades();
            foreach (Trade trade in trades)
            {
                ClosedTrades.Add(trade);
            }
        }

        private void UpdateBalance(IProvider provider)
        {
            IReadOnlyCollection<CashAmount> balances = provider.GetCashBalance();
            foreach (CashAmount balance in balances)
            {
                bool update = false;
                foreach (BalanceViewModel vm in Balances)
                {
                    if (vm.Currency == balance.Currency)
                    {
                        vm.Update(balance);
                        update = true;
                        break;
                    }
                }

                if (!update)
                {
                    Balances.Add(new BalanceViewModel(balance));
                }
            }

            BalanceViewModel[] vms = new BalanceViewModel[Balances.Count];
            Balances.CopyTo(vms, 0);
            foreach (BalanceViewModel vm in vms)
            {
                CashAmount balance = balances.FirstOrDefault(m => m.Currency == vm.Currency);
                if (balance == null || balance.Currency == null)
                {
                    Balances.Remove(vm);
                }
            }
        }
    }
}