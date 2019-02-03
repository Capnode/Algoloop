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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Algoloop.Model;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using QuantConnect;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Fxcm;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace Algoloop.ViewModel
{
    public class AccountViewModel : ViewModelBase
    {
        private AccountsViewModel _parent;
        private CancellationTokenSource _connectCancel;
        private FxcmBrokerage _brokerage;
        private CancellationTokenSource _disconnectCancel;
        private const string FxcmServer = "http://www.fxcorporate.com/Hosts.jsp";

        public AccountViewModel(AccountsViewModel accountsViewModel, AccountModel accountModel)
        {
            _parent = accountsViewModel;
            Model = accountModel;

            ActiveCommand = new RelayCommand(() => OnActiveCommand(Model.Active), true);
            StartCommand = new RelayCommand(async () => await ConnectAsync(), () => !Active);
            StopCommand = new RelayCommand(async () => await DisconnectAsync(), () => Active);
            DeleteCommand = new RelayCommand(() => _parent?.DeleteAccount(this), () => !Active);
        }

        public AccountModel Model { get; }
        public RelayCommand ActiveCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public SyncObservableCollection<OrderViewModel> Orders { get; } = new SyncObservableCollection<OrderViewModel>();
        public SyncObservableCollection<PositionViewModel> Positions { get; } = new SyncObservableCollection<PositionViewModel>();
        public SyncObservableCollection<BalanceViewModel> Balances { get; } = new SyncObservableCollection<BalanceViewModel>();

        public bool Active
        {
            get => Model.Active;
            set
            {
                if (Model.Active != value)
                {
                    Model.Active = value;
                    RaisePropertyChanged(() => Active);
                    StartCommand.RaiseCanExecuteChanged();
                    StopCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        internal async Task ConnectAsync()
        {
            if (_connectCancel != null || _disconnectCancel != null)
            {
                Log.Error($"{_brokerage.Name}: Busy");
                return;
            }

            Active = true;
            Log.Trace($"Connect Account {Model.Name}");
            _connectCancel = new CancellationTokenSource();
            try
            {
                _brokerage = new FxcmBrokerage(null, null, FxcmServer, Model.Access.ToString(), Model.Login, Model.Password, Model.Id);
                _brokerage.AccountChanged += OnAccountChanged;
                _brokerage.OptionPositionAssigned += OnOptionPositionAssigned;
                _brokerage.OrderStatusChanged += OnOrderStatusChanged;
                _brokerage.Message += OnMessage;

                await Task.Run(() => _brokerage.Connect(), _connectCancel.Token);

                // Update Orders
                List<Order> openOrders = _brokerage.GetOpenOrders();
                foreach (Order openOrder in openOrders)
                {
                    bool update = false;
                    foreach (OrderViewModel order in Orders)
                    {
                        if (openOrder.Id == order.Id)
                        {
                            order.Update(openOrder);
                            update = true;
                            break;
                        }
                    }

                    if (!update)
                    {
                        Orders.Add(new OrderViewModel(openOrder));
                    }
                }

                // Set Positions
                Positions.Clear();
                List<Holding> holdings = _brokerage.GetAccountHoldings();
                foreach (var holding in holdings)
                {
                    Positions.Add(new PositionViewModel(holding));
                }

                // Set Balance
                Balances.Clear();
                List<CashAmount> balances = _brokerage.GetCashBalance();
                foreach (var balance in balances)
                {
                    Balances.Add(new BalanceViewModel(balance));
                }

                Active = true;
                _connectCancel = null;
            }
            catch (Exception ex)
            {
                Log.Error($"{_brokerage.Name}: {ex.GetType()}: {ex.Message}");
                await DisconnectAsync();
            }
        }

        internal async Task DisconnectAsync()
        {
            if (_connectCancel != null || _disconnectCancel != null)
            {
                Log.Error($"{_brokerage.Name}: Busy");
                return;
            }

            Active = false;
            if (_brokerage == null)
            {
                return;
            }

            _connectCancel?.Cancel();

            _disconnectCancel = new CancellationTokenSource();
            await Task.Run(() => _brokerage.Disconnect(), _disconnectCancel.Token);
            Positions.Clear();
            Balances.Clear();
            _brokerage = null;
            _disconnectCancel = null;
        }

        private async void OnActiveCommand(bool value)
        {
            if (value)
            {
                await ConnectAsync();
            }
            else
            {
                await DisconnectAsync();
            }
        }

        private void OnMessage(object sender, BrokerageMessageEvent message)
        {
            var brokerage = sender as Brokerage;
            Log.Trace($"{brokerage.Name}: {message.GetType()}: {message}");
        }

        private void OnOrderStatusChanged(object sender, OrderEvent message)
        {
            var brokerage = sender as Brokerage;
            Log.Trace($"{brokerage.Name}: {message.GetType()}: {message}");

            // Update Orders
            bool update = false;
            foreach (OrderViewModel order in Orders)
            {
                if (message.OrderId == order.Id)
                {
                    order.Update(message);
                    update = true;
                    break;
                }
            }

            if (!update)
            {
                Orders.Add(new OrderViewModel(message));
            }
        }

        private void OnOptionPositionAssigned(object sender, OrderEvent message)
        {
            var brokerage = sender as Brokerage;
            Log.Trace($"{brokerage.Name}: {message.GetType()}: {message}");
        }

        private void OnAccountChanged(object sender, AccountEvent message)
        {
            var brokerage = sender as Brokerage;
            Log.Trace($"{brokerage.Name}: {message.GetType()}: {message}");

            // Update balance
            bool update = false;
            foreach (BalanceViewModel balance in Balances)
            {
                if (message.CurrencySymbol == balance.Currency)
                {
                    balance.Update(message);
                    update = true;
                    break;
                }
            }

            if (!update)
            {
                Balances.Add(new BalanceViewModel(message));
            }
        }
    }
}