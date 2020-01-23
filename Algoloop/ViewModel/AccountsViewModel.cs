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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Algoloop.Model;
using Algoloop.Service;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;

namespace Algoloop.ViewModel
{
    public class AccountsViewModel : ViewModelBase
    {
        private ITreeViewModel _selectedItem;
        private bool _isBusy;

        public AccountsViewModel(AccountService accounts)
        {
            Model = accounts;
            AddCommand = new RelayCommand(() => AddAccount(), () => !IsBusy);
            SelectedChangedCommand = new RelayCommand<ITreeViewModel>((market) => DoSelectedChanged(market), (market) => market != null);
            DataFromModel();
        }

        public RelayCommand<ITreeViewModel> SelectedChangedCommand { get; }
        public RelayCommand AddCommand { get; }

        public AccountService Model { get; }
        public SyncObservableCollection<AccountViewModel> Accounts { get; } = new SyncObservableCollection<AccountViewModel>();

        /// <summary>
        /// Mark ongoing operation
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => Set(ref _isBusy, value);
        }

        public ITreeViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                Set(ref _selectedItem, value);
            }
        }

        public bool DoDeleteAccount(AccountViewModel account)
        {
            try
            {
                IsBusy = true;
                Debug.Assert(account != null);
                SelectedItem = null;
                return Accounts.Remove(account);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool Read(string fileName)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    using (StreamReader r = new StreamReader(fileName))
                    {
                        string json = r.ReadToEnd();
                        Model.Copy(JsonConvert.DeserializeObject<AccountService>(json));
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().ToString());
                    return false;
                }
            }

            DataFromModel();
            StartTasks();
            return true;
        }

        internal bool Save(string fileName)
        {
            try
            {
                DataToModel();

                using StreamWriter file = File.CreateText(fileName);
                JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.Indented };
                serializer.Serialize(file, Model);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
                return false;
            }
        }

        private void DoSelectedChanged(ITreeViewModel vm)
        {
            try
            {
                vm.Refresh();
                SelectedItem = vm;
            }
            finally
            {
                IsBusy = false;
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
                    _ = account.DoConnectAsync();
                }
            }
        }
    }
}
