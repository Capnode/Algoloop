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
using Algoloop.Wpf.Properties;
using Algoloop.Wpf.ViewSupport;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using QuantConnect;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Fxcm;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Statistics;
using QuantConnect.Util;

namespace Algoloop.Wpf.ViewModel
{
    public class AccountViewModel : ViewModel, ITreeViewModel, IDisposable
    {
        private bool _isDisposed = false; // To detect redundant calls
        private readonly AccountsViewModel _parent;
        private CancellationTokenSource _cancel;
        private FxcmBrokerage _brokerage;
        private Task _task;
        private IList _selectedItems;
        private const string FxcmServer = "http://www.fxcorporate.com/Hosts.jsp";

        public AccountViewModel(AccountsViewModel accountsViewModel, AccountModel accountModel)
        {
            _parent = accountsViewModel;
            Model = accountModel;

            ActiveCommand = new RelayCommand(() => DoActiveCommand(Model.Active), () => !IsBusy);
            StartCommand = new RelayCommand(async () => await DoConnectAsync().ConfigureAwait(false), () => !IsBusy && !Active);
            StopCommand = new RelayCommand(async () => await DoDisconnectAsync().ConfigureAwait(false), () => !IsBusy && Active);
            DeleteCommand = new RelayCommand(() => _parent?.DoDeleteAccount(this), () => !IsBusy && !Active);

            DataFromModel();
            Debug.Assert(IsUiThread(), "Not UI thread!");
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

        public RelayCommand ActiveCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand DeleteCommand { get; }

        public AccountModel Model { get; }
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
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        public void Refresh()
        {
            Model.Refresh();
        }

        internal async Task DoConnectAsync()
        {
            if (_cancel != null || _task != null)
            {
                return;
            }

            try
            {
                IsBusy = true;
                Active = true;
                Log.Trace($"Connect Account {Model.Name}");
                try
                {
                    _brokerage = new FxcmBrokerage(
                        null, 
                        null, 
                        Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager")),
                        FxcmServer,
                        Model.Access.ToString(),
                        Model.Login,
                        Model.Password,
                        Model.Id);
                    _brokerage.Message += OnMessage;
                    _brokerage.AccountChanged += OnAccountChanged;
                    _brokerage.OptionPositionAssigned += OnOptionPositionAssigned;
                    _brokerage.OrderStatusChanged += OnOrderStatusChanged;
                    _cancel = new CancellationTokenSource();
                    _task = Task.Run(() => MainLoop(), _cancel.Token);
                }
                catch (Exception ex)
                {
                    Log.Error($"{_brokerage.Name}: {ex.GetType()}: {ex.Message}");
                }

                if (!Active)
                {
                    await DoDisconnectAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal async Task DoDisconnectAsync()
        {
            if (_cancel == null || _task == null)
            {
                return;
            }

            try
            {
                IsBusy = true;
                Active = false;
                _cancel.Cancel();
                await _task.ConfigureAwait(false);
                _cancel = null;
                _task = null;

                if (Active)
                {
                    await DoConnectAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal void DataToModel()
        {
            try
            {
                IsBusy = true;
                Model.Orders.Clear();
                foreach (OrderViewModel vm in Orders)
                {
                    Model.Orders.Add(vm.Model);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal void DataFromModel()
        {
            try
            {
                IsBusy = true;
                Orders.Clear();
                foreach (OrderModel order in Model.Orders)
                {
                    var vm = new OrderViewModel(order);
                    Orders.Add(vm);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void MainLoop()
        {
            _brokerage.Connect();
            bool loop = true;
            while (loop)
            {
                UpdateOrder();
                UpdatePosition();
                UpdateBalance();
                UpdateClosedTrades();
                loop = !_cancel.Token.WaitHandle.WaitOne(1000);
            }

            _brokerage.Disconnect();
            Positions.Clear();
            Balances.Clear();
        }

        private void UpdateOrder()
        {
            List<Order> orders = _brokerage.GetOpenOrders();
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

        private void UpdatePosition()
        {
            List<Holding> holdings = _brokerage.GetAccountHoldings();
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
                Holding holding = holdings.Find(m => m.Symbol.Value == vm.Symbol);
                if (holding == null || holding.Symbol == null)
                {
                    Positions.Remove(vm);
                }
            }
        }

        private void UpdateClosedTrades()
        {
            ClosedTrades.Clear();
            List<Trade> trades = _brokerage.GetClosedTrades();
            foreach (Trade trade in trades)
            {
                ClosedTrades.Add(trade);
            }
        }

        private void UpdateBalance()
        {
            List<CashAmount> balances = _brokerage.GetCashBalance();
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
                CashAmount balance = balances.Find(m => m.Currency == vm.Currency);
                if (balance == null || balance.Currency == null)
                {
                    Balances.Remove(vm);
                }
            }
        }

        private async void DoActiveCommand(bool value)
        {
            if (value)
            {
                await DoConnectAsync().ConfigureAwait(false);
            }
            else
            {
                await DoDisconnectAsync().ConfigureAwait(false);
            }
        }

        private void OnMessage(object sender, BrokerageMessageEvent message)
        {
            var brokerage = sender as Brokerage;
            Log.Trace($"{brokerage.Name}: {message.GetType()}: {message}");
        }

        private void OnAccountChanged(object sender, AccountEvent e)
        {
            var brokerage = sender as Brokerage;
            Log.Trace($"{brokerage.Name}: {e.GetType()}: {e}");
        }

        private void OnOrderStatusChanged(object sender, OrderEvent e)
        {
            var brokerage = sender as Brokerage;
            Log.Trace($"{brokerage.Name}: {e.GetType()}: {e}");
        }

        private void OnOptionPositionAssigned(object sender, OrderEvent e)
        {
            var brokerage = sender as Brokerage;
            Log.Trace($"{brokerage.Name}: {e.GetType()}: {e}");
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
                    _brokerage.Dispose();
                    _task.Dispose();
                    _cancel.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}