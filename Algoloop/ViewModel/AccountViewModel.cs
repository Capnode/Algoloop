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

namespace Algoloop.ViewModel
{
    public class AccountViewModel : ViewModelBase
    {
        private AccountsViewModel _parent;
        private CancellationTokenSource _cancel;
        private const string FxcmServer = "http://www.fxcorporate.com/Hosts.jsp";

        public AccountViewModel(AccountsViewModel accountsViewModel, AccountModel accountModel)
        {
            _parent = accountsViewModel;
            Model = accountModel;

            ActiveCommand = new RelayCommand(() => OnActiveCommand(Model.Active), true);
            StartCommand = new RelayCommand(() => OnStartCommand(), () => !Active);
            StopCommand = new RelayCommand(() => StopTask(), () => Active);
            DeleteCommand = new RelayCommand(() => _parent?.DeleteAccount(this), () => !Active);
        }

        public AccountModel Model { get; }

        public RelayCommand ActiveCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand DeleteCommand { get; }

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

        public SyncObservableCollection<PositionViewModel> Positions { get; } = new SyncObservableCollection<PositionViewModel>();
        public SyncObservableCollection<BalanceViewModel> Balances { get; } = new SyncObservableCollection<BalanceViewModel>();

        internal async Task StartTask()
        {
            Log.Trace($"Connect Account {Model.Name}");
            _cancel = new CancellationTokenSource();
            await Task.Run(() => StartFxcm(_cancel.Token), _cancel.Token);
            _cancel = null;
            Log.Trace($"Disconnect Account {Model.Name}");
            Active = false;
        }

        internal void StopTask()
        {
            if (_cancel != null)
            {
                _cancel.Cancel();
            }
        }

        private async void OnActiveCommand(bool value)
        {
            if (value)
            {
                await StartTask();
            }
            else
            {
                StopTask();
            }
        }

        private async void OnStartCommand()
        {
            Active = true;
            await StartTask();
        }

        private void StartFxcm(CancellationToken cancel)
        {
            Brokerage brokerage = null;
            try
            {
                brokerage = new FxcmBrokerage(null, null, FxcmServer, Model.Access.ToString(), Model.Login, Model.Password, Model.Id);
                brokerage.AccountChanged += OnAccountChanged;
                brokerage.OptionPositionAssigned += OnOptionPositionAssigned;
                brokerage.OrderStatusChanged += OnOrderStatusChanged;
                brokerage.Message += OnMessage;
                brokerage.Connect();

                List<QuantConnect.Orders.Order> orders = brokerage.GetOpenOrders();

                bool stop = false;
                while (!stop)
                {
                    // Set Positions
                    List<Holding> holdings = brokerage.GetAccountHoldings();
                    if (Positions.Count != holdings.Count)
                    {
                        Positions.Clear();
                        holdings.ForEach(m => Positions.Add(new PositionViewModel(m)));
                    }
                    else
                    {
                        int i = 0;
                        foreach (var holding in holdings)
                        {
                            Positions[i++].Update(holding);
                        }
                    }

                    // Set Balance
                    List<QuantConnect.Securities.Cash> balances = brokerage.GetCashBalance();
                    if (Balances.Count != balances.Count)
                    {
                        Balances.Clear();
                        balances.ForEach(m => Balances.Add(new BalanceViewModel(m)));
                    }
                    else
                    {
                        int i = 0;
                        foreach (var balance in balances)
                        {
                            Balances[i++].Update(balance);
                        }
                    }

                    // Tick data

                    stop = cancel.WaitHandle.WaitOne(1000);
                }

                brokerage.Disconnect();
                Positions.Clear();
                Balances.Clear();
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.GetType()}: {ex.Message}");
                if (brokerage != null)
                {
                    brokerage.Disconnect();
                }
            }
        }

        private void OnMessage(object sender, QuantConnect.Brokerages.BrokerageMessageEvent e)
        {
            Log.Trace($"{e.GetType()}: {e.Code} {e.Message}");
        }

        private void OnOrderStatusChanged(object sender, QuantConnect.Orders.OrderEvent e)
        {
            Log.Trace($"{e.GetType()}: {e.Message}");
        }

        private void OnOptionPositionAssigned(object sender, QuantConnect.Orders.OrderEvent e)
        {
            Log.Trace($"{e.GetType()}: {e.Message}");
        }

        private void OnAccountChanged(object sender, QuantConnect.Securities.AccountEvent e)
        {
            Log.Trace($"{e.GetType()}: {e.CurrencySymbol} {e.CashBalance}");
        }
    }
}