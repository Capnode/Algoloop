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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Algoloop.Model;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;

namespace Algoloop.ViewModel
{
    public class AccountsViewModel : ViewModelBase
    {
        private ITreeViewModel _selectedItem;
        private bool _isBusy;
        private static readonly AccountModel[] _standardAccounts = new []
        {
            new AccountModel() { Name = AccountModel.AccountType.Backtest.ToString() },
            new AccountModel() { Name = AccountModel.AccountType.Paper.ToString() }
        };

        public AccountsViewModel(AccountsModel model)
        {
            Model = model;
            AddCommand = new RelayCommand(() => AddAccount(), () => !IsBusy);
            SelectedChangedCommand = new RelayCommand<ITreeViewModel>((market) => OnSelectedChanged(market), (market) => market != null);
            Messenger.Default.Register<NotificationMessageAction<List<AccountModel>>>(this, (message) => OnNotificationMessage(message));
            DataFromModel();
        }

        public RelayCommand<ITreeViewModel> SelectedChangedCommand { get; }
        public RelayCommand AddCommand { get; }

        public AccountsModel Model { get; }
        public SyncObservableCollection<AccountViewModel> Accounts { get; } = new SyncObservableCollection<AccountViewModel>();

        /// <summary>
        /// Mark ongoing operation
        /// </summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                RaisePropertyChanged("IsBusy");
            }
        }

        public ITreeViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                Set(ref _selectedItem, value);
            }
        }

        internal bool DeleteAccount(AccountViewModel account)
        {
            Debug.Assert(account != null);
            SelectedItem = null;
            return Accounts.Remove(account);
        }

        internal bool Read(string fileName)
        {
            if (!File.Exists(fileName))
                return false;

            try
            {
                using (StreamReader r = new StreamReader(fileName))
                {
                    string json = r.ReadToEnd();
                    Model.Copy(JsonConvert.DeserializeObject<AccountsModel>(json));
                }

                DataFromModel();
                StartTasks();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
                return false;
            }
        }

        internal bool Save(string fileName)
        {
            try
            {
                DataToModel();

                using (StreamWriter file = File.CreateText(fileName))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, Model);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
                return false;
            }
        }

        private void OnSelectedChanged(ITreeViewModel vm)
        {
            vm.Refresh();
            SelectedItem = vm;
        }

        private void OnNotificationMessage(NotificationMessageAction<List<AccountModel>> message)
        {
            IEnumerable<AccountModel> accounts = Accounts.Select(m => m.Model).Concat(_standardAccounts);
            if (string.IsNullOrEmpty(message.Notification))
            {
                message.Execute(accounts.ToList());
            }
            else
            {
                message.Execute(accounts.Where(s => s.Name.Equals(message.Notification)).ToList());
            }
        }

        private void AddAccount()
        {
            var loginViewModel = new AccountViewModel(this, new AccountModel());
            Accounts.Add(loginViewModel);
        }

        private void DataToModel()
        {
            Model.Accounts.Clear();
            foreach (AccountViewModel account in Accounts)
            {
                Model.Accounts.Add(account.Model);
                account.DataToModel();
            }
        }

        private void DataFromModel()
        {
            Accounts.Clear();
            foreach (AccountModel account in Model.Accounts)
            {
                var loginViewModel = new AccountViewModel(this, account);
                Accounts.Add(loginViewModel);
            }
        }

        private void StartTasks()
        {
            foreach (AccountViewModel account in Accounts)
            {
                if (account.Active)
                {
                    Task task = account.ConnectAsync();
                }
            }
        }
    }
}
