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

using System.Diagnostics;
using System.IO;
using System.Linq;
using Algoloop.Model;
using Algoloop.Wpf.ViewSupport;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using QuantConnect.Logging;

namespace Algoloop.Wpf.ViewModel
{
    public class AccountsViewModel : ViewModel
    {
        private readonly SettingModel _settings;
        private ITreeViewModel _selectedItem;
        private bool _isBusy;

        public AccountsViewModel(AccountsModel accounts, SettingModel settings)
        {
            Model = accounts;
            _settings = settings;

            AddCommand = new RelayCommand(() => AddBroker(), () => !IsBusy);
            SelectedChangedCommand = new RelayCommand<ITreeViewModel>((market) => DoSelectedChanged(market), (market) => !IsBusy && market != null);
            DataFromModel();
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public RelayCommand<ITreeViewModel> SelectedChangedCommand { get; }
        public RelayCommand AddCommand { get; }

        public AccountsModel Model { get; }

        public SyncObservableCollection<BrokerViewModel> Brokers { get; } = new SyncObservableCollection<BrokerViewModel>();

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

        public bool DoDeleteBroker(BrokerViewModel broker)
        {
            try
            {
                IsBusy = true;
                Debug.Assert(broker != null);
                SelectedItem = null;
                return Brokers.Remove(broker);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Read(string fileName)
        {
            Log.Trace($"Reading {fileName}");
            if (File.Exists(fileName))
            {
                using StreamReader r = new StreamReader(fileName);
                string json = r.ReadToEnd();
                AccountsModel accounts = JsonConvert.DeserializeObject<AccountsModel>(json);
                Model.Copy(accounts);
            }

            DataFromModel();
            StartTasks();
        }

        internal void Save(string fileName)
        {
            DataToModel();

            // Do not overwrite if file read error
            if (!Model.Brokers.Any()) return;

            using StreamWriter file = File.CreateText(fileName);
            JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.Indented };
            serializer.Serialize(file, Model);
        }

        private void DoSelectedChanged(ITreeViewModel vm)
        {
            // No IsBusy here
            vm.Refresh();
            SelectedItem = vm;
        }

        private void AddBroker()
        {
            var broker = new BrokerViewModel(this, new BrokerModel(), _settings);
            Brokers.Add(broker);
        }

        private void DataToModel()
        {
            Model.Version = AccountsModel.version;
            Model.Brokers.Clear();
            foreach (BrokerViewModel broker in Brokers)
            {
                Model.Brokers.Add(broker.Model);
                broker.DataToModel();
            }
        }

        private void DataFromModel()
        {
            Brokers.Clear();
            foreach (BrokerModel broker in Model.Brokers)
            {
                var vm = new BrokerViewModel(this, broker, _settings);
                Brokers.Add(vm);
            }
        }

        private void StartTasks()
        {
            foreach (BrokerViewModel broker in Brokers)
            {
                if (broker.Active)
                {
                    _ = broker.StartAccountAsync();
                }
            }
        }
    }
}
