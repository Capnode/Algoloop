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
        private AccountViewModel _selectedItem;

        public AccountsViewModel(AccountsModel model)
        {
            Model = model;
            AddCommand = new RelayCommand(() => AddAccount(), true);
            DeleteCommand = new RelayCommand<AccountViewModel>((account) => DeleteAccount(account), (account) => account != null);
            SelectedChangedCommand = new RelayCommand<AccountViewModel>((market) => OnSelectedChanged(market), (market) => market != null);
            Messenger.Default.Register<NotificationMessageAction<List<AccountModel>>>(this, (message) => OnNotificationMessage(message));
            DataFromModel();
        }

        public AccountsModel Model { get; }

        public SyncObservableCollection<AccountViewModel> Accounts { get; } = new SyncObservableCollection<AccountViewModel>();

        public RelayCommand AddCommand { get; }

        public RelayCommand<AccountViewModel> DeleteCommand { get; }
        public RelayCommand<AccountViewModel> SelectedChangedCommand { get; }

        public AccountViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                Set(ref _selectedItem, value);
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnSelectedChanged(AccountViewModel account)
        {
            SelectedItem = account;
        }

        private void OnNotificationMessage(NotificationMessageAction<List<AccountModel>> message)
        {
            if (string.IsNullOrEmpty(message.Notification))
            {
                message.Execute(Accounts.Select(m => m.Model).ToList());
            }
            else
            {
                message.Execute(Accounts.Where(s => s.Model.Name.Equals(message.Notification)).Select(m => m.Model).ToList());
            }
        }

        private void AddAccount()
        {
            var loginViewModel = new AccountViewModel(this, new AccountModel());
            Accounts.Add(loginViewModel);
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

        private void DataToModel()
        {
            Model.Accounts.Clear();
            foreach (AccountViewModel account in Accounts)
            {
                Model.Accounts.Add(account.Model);
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
