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
using Algoloop.Model;
using Algoloop.Wpf.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using QuantConnect.Logging;

namespace Algoloop.Wpf.ViewModel
{
    public class AccountsViewModel : ViewModelBase
    {
        private ITreeViewModel _selectedItem;
        private bool _isBusy;

        public AccountsViewModel(AccountsModel accounts)
        {
            Model = accounts;
            AddCommand = new RelayCommand(() => AddAccount(), () => !IsBusy);
            SelectedChangedCommand = new RelayCommand<ITreeViewModel>((market) => DoSelectedChanged(market), (market) => !IsBusy && market != null);
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
            Log.Trace($"Reading {fileName}");
            if (File.Exists(fileName))
            {
                try
                {
                    using StreamReader r = new StreamReader(fileName);
                    string json = r.ReadToEnd();
                    Model.Copy(JsonConvert.DeserializeObject<AccountsModel>(json));

                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed reading {fileName}\n");
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
                Log.Error(ex, $"Failed reading {fileName}\n");
                return false;
            }
        }

        private void DoSelectedChanged(ITreeViewModel vm)
        {
            // No IsBusy here
            vm.Refresh();
            SelectedItem = vm;
        }

        private void AddAccount()
        {
            var loginViewModel = new AccountViewModel(this, new AccountModel());
            Accounts.Add(loginViewModel);
        }

        private void DataToModel()
        {
            Model.Version = AccountsModel.version;
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
