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

using Algoloop.Model;
using Microsoft.Toolkit.Mvvm.Input;
using Newtonsoft.Json;
using QuantConnect.Logging;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Algoloop.ViewModel
{
    public class MarketsViewModel : ViewModelBase
    {
        private readonly SettingModel _settings;
        private ITreeViewModel _selectedItem;
        private bool _isBusy;

        public MarketsViewModel(MarketsModel markets, SettingModel settings)
        {
            Model = markets;
            _settings = settings;

            AddCommand = new RelayCommand(() => DoAddMarket(), () => !IsBusy);
            SelectedChangedCommand = new RelayCommand<ITreeViewModel>(
                (vm) => DoSelectedChanged(vm), (vm) => !IsBusy && vm != null);

            DataFromModel();
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public RelayCommand<ITreeViewModel> SelectedChangedCommand { get; }
        public RelayCommand AddCommand { get; }

        public MarketsModel Model { get; set; }
        public SyncObservableCollection<MarketViewModel> Markets { get; } = new SyncObservableCollection<MarketViewModel>();

        /// <summary>
        /// Mark ongoing operation
        /// </summary>
        public bool IsBusy
        {
            get =>_isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ITreeViewModel SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        internal bool DoDeleteMarket(MarketViewModel market)
        {
            Debug.Assert(market != null);
            SelectedItem = null;
            market.StopMarket();
            Debug.Assert(!market.Active);
            return Markets.Remove(market);
        }

        internal async Task<bool> ReadAsync(string fileName)
        {
            Log.Trace($"Reading {fileName}");
            if (File.Exists(fileName))
            {
                await Task.Run(() =>
                {
                    using var r = new StreamReader(fileName);
                    using var reader = new JsonTextReader(r);
                    var serializer = new JsonSerializer();
                    Model.Copy(serializer.Deserialize<MarketsModel>(reader));
                }).ConfigureAwait(true); // Must continue on UI thread
            }

            DataFromModel();
            return true;
        }

        internal void Save(string fileName)
        {
            DataToModel();

            // Do not overwrite if file read error
            if (!Model.Markets.Any()) return;

            using StreamWriter file = File.CreateText(fileName);
            var serializer = new JsonSerializer { Formatting = Formatting.Indented };
            serializer.Serialize(file, Model);
        }

        private void DoSelectedChanged(ITreeViewModel vm)
        {
            // No IsBusy here
            vm?.Refresh();
            SelectedItem = vm;
        }

        private void DoAddMarket()
        {
            try
            {
                IsBusy = true;
                var loginViewModel = new MarketViewModel(this, new ProviderModel(), _settings);
                Markets.Add(loginViewModel);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DataToModel()
        {
            Model.Version = MarketsModel.version;
            Model.Markets.Clear();
            foreach (MarketViewModel market in Markets)
            {
                Model.Markets.Add(market.Model);
                market.DataToModel();
            }
        }

        private void DataFromModel()
        {
            Debug.Assert(IsUiThread());
            Markets.Clear();
            foreach (ProviderModel market in Model.Markets)
            {
                foreach (AccountModel account in market.Accounts)
                {
                    account.Provider = market;
                }

                var viewModel = new MarketViewModel(this, market, _settings);
                Markets.Add(viewModel);
            }
        }
    }
}
