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
using QuantConnect.Logging;

namespace Algoloop.Wpf.ViewModel
{
    public class BrokerViewModel : ViewModel, ITreeViewModel, IDisposable
    {
        private bool _isDisposed = false; // To detect redundant calls
        private readonly AccountsViewModel _parent;
        private readonly SettingModel _settings;
        private CancellationTokenSource _cancel;
        private IList _selectedItems;
        private ProviderModel _model;

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

        public BrokerViewModel(AccountsViewModel accountsViewModel, ProviderModel accountModel, SettingModel settings)
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

        public ProviderModel Model
        {
            get => _model;
            set => Set(ref _model, value);
        }

        public SyncObservableCollection<AccountViewModel> Accounts { get; } = new SyncObservableCollection<AccountViewModel>();

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
            Model.Accounts.Clear();
            foreach (AccountViewModel account in Accounts)
            {
                Model.Accounts.Add(account.Model);
                account.DataToModel();
            }
        }

        internal void DataFromModel()
        {
            Accounts.Clear();
            foreach (AccountModel account in Model.Accounts)
            {
                var viewModel = new AccountViewModel(this, account);
                Accounts.Add(viewModel);
            }
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
            Model.UpdateAccounts(accounts);
            UiThread(() => DataFromModel());
            while (!_cancel.IsCancellationRequested)
            {
                UpdateOrder(provider, accounts);
                UpdatePosition(provider, accounts);
                UpdateBalance(provider, accounts);
                UpdateClosedTrades(provider, accounts);
                Thread.Sleep(1000);
            }

            provider.Logout();
        }

        private static void UpdateOrder(IProvider provider, IEnumerable<AccountModel> accounts)
        {
            Contract.Requires(provider != null);
            Contract.Requires(accounts != null);
        }

        private static void UpdatePosition(IProvider provider, IEnumerable<AccountModel> accounts)
        {
            Contract.Requires(provider != null);
            Contract.Requires(accounts != null);
        }

        private static void UpdateClosedTrades(IProvider provider, IEnumerable<AccountModel> accounts)
        {
            Contract.Requires(provider != null);
            Contract.Requires(accounts != null);
        }

        private static void UpdateBalance(IProvider provider, IEnumerable<AccountModel> accounts)
        {
            Contract.Requires(provider != null);
            Contract.Requires(accounts != null);
        }
    }
}