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
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using QuantConnect.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace Algoloop.Wpf.ViewModels
{
    public class MarketsViewModel : ViewModelBase
    {
        private const string MarketsFile = "Markets.json";
        private const string BackupFile = "Markets.bak";
        private const string TempFile = "Markets.tmp";

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
            market.Active = false;
            Debug.Assert(!market.Active);
            return Markets.Remove(market);
        }

        internal bool Read(string folder)
        {
            if (!Directory.Exists(folder)) throw new ArgumentException($"Can not find Folder: {folder}");
            if (ReadFile(Path.Combine(folder, MarketsFile))) return true;
            if (ReadFile(Path.Combine(folder, BackupFile))) return true;
            return false;
        }

        internal void Save(string folder)
        {
            if (!Directory.Exists(folder)) throw new ArgumentException($"Can not find Folder: {folder}");
            string fileName = Path.Combine(folder, MarketsFile);
            string backupFile = Path.Combine(folder, BackupFile);
            string tempFile = Path.Combine(folder, TempFile);
            if (File.Exists(fileName))
            {
                File.Copy(fileName, tempFile, true);
            }

            DataToModel();
            SaveFile(fileName);
            File.Move(tempFile, backupFile, true);
        }

        internal SymbolViewModel FindSymbol(string marketName, string symbolName)
        {
            foreach (MarketViewModel market in Markets)
            {
                if (market.Model.Provider != marketName) continue;
                var symbol = market.FindSymbol(symbolName);
                if (symbol != null) return symbol;
            }

            return null;
        }

        private bool ReadFile(string fileName)
        {
            Log.Trace($"Reading {fileName}");
            try
            {
                using var r = new StreamReader(fileName);
                using var reader = new JsonTextReader(r);
                var serializer = new JsonSerializer();
                MarketsModel model = serializer.Deserialize<MarketsModel>(reader);
                Model.Copy(model);
                DataFromModel();
                return true;
            }
            catch (Exception ex)
            {
                Log.Trace($"Failed reading {fileName}: {ex.Message}", true);
                return false;
            }
        }

        private void SaveFile(string fileName)
        {
            Log.Trace($"Writing {fileName}");
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
            Model.Version = MarketsModel.ActualVersion;
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
