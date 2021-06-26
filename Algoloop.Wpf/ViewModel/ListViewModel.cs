/*
 * Copyright 2019 Capnode AB
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
using Algoloop.Wpf.Properties;
using Algoloop.Wpf.ViewSupport;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using QuantConnect.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace Algoloop.Wpf.ViewModel
{
    public class ListViewModel : ViewModel, ITreeViewModel
    {
        private SymbolViewModel _selectedSymbol;
        private SymbolViewModel _marketSymbol;
        private IList _selectedItems;

        public ListViewModel(MarketViewModel market, ListModel model)
        {
            Market = market ?? throw new ArgumentNullException(nameof(market));
            Model = model;

            DeleteCommand = new RelayCommand(() => Market?.DeleteList(this), () => !IsBusy && !Market.Active);
            StartCommand = new RelayCommand(() => { }, () => false);
            StopCommand = new RelayCommand(() => { }, () => false);
            AddSymbolCommand = new RelayCommand<SymbolViewModel>(m => DoAddSymbol(m), m => !IsBusy && !Market.Active && MarketSymbols.View.Cast<object>().FirstOrDefault() != null);
            RemoveSymbolsCommand = new RelayCommand<IList>(m => DoRemoveSymbols(m), m => !IsBusy && !Market.Active && SelectedSymbol != null);
            ExportListCommand = new RelayCommand(() => DoExportList(), () => !IsBusy && !Market.Active);

            DataFromModel();

            MarketSymbols.Filter += (object sender, FilterEventArgs e) =>
            {
                if (e.Item is SymbolViewModel marketSymbol)
                {
                    e.Accepted = marketSymbol.Active
                    && !Symbols.Any(m => m.Model.Id.Equals(marketSymbol.Model.Id, StringComparison.OrdinalIgnoreCase));
                }
            };

            MarketSymbols.Source = Market.Symbols;
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public bool IsBusy
        {
            get => Market.IsBusy;
            set => Market.IsBusy = value;
        }

        public ITreeViewModel SelectedItem
        {
            get => Market.SelectedItem;
            set => Market.SelectedItem = value;
        }

        public RelayCommand DeleteCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand<SymbolViewModel> AddSymbolCommand { get; }
        public RelayCommand<IList> RemoveSymbolsCommand { get; }
        public RelayCommand ExportListCommand { get; }
        public string DisplayName => Model != null ? $"{Market.Model.Name}: {Model.Name} ({Symbols.Count})" : string.Empty;

        public MarketViewModel Market { get; }
        public ListModel Model { get; }
        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();
        public CollectionViewSource MarketSymbols { get; } = new CollectionViewSource();

        public SymbolViewModel MarketSymbol
        {
            get => _marketSymbol;
            set => Set(ref _marketSymbol, value);
        }

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

        public SymbolViewModel SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                Set(ref _selectedSymbol, value);
                RemoveSymbolsCommand.RaiseCanExecuteChanged();
            }
        }

        public void Refresh()
        {
            Model.Refresh();
            DataFromModel();
            MarketSymbols.View.Refresh();
            AddSymbolCommand.RaiseCanExecuteChanged();
        }

        public void AddSymbols(IEnumerable<SymbolViewModel> symbols)
        {
            if (symbols == null) throw new ArgumentNullException(nameof(symbols));

            foreach (SymbolViewModel symbol in symbols)
            {
                if (!Symbols.Contains(symbol))
                {
                    Symbols.Add(symbol);
                }
            }

            DataToModel();
            MarketSymbols.View.Refresh();
            AddSymbolCommand.RaiseCanExecuteChanged();
        }

        private void DoAddSymbol(SymbolViewModel symbol)
        {
            if (symbol == null)
                return;

            try
            {
                IsBusy = true;
                AddSymbols(new List<SymbolViewModel> { symbol });
                DataToModel();
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal void DataToModel()
        {
            Model.Symbols.Clear();
            foreach (SymbolViewModel symbol in Symbols)
            {
                Model.Symbols.Add(symbol.Model);
            }
        }

        internal void DataFromModel()
        {
            Symbols.Clear();
            foreach (SymbolViewModel marketSymbol in Market.Symbols)
            {
                if (marketSymbol.Active)
                {
                    SymbolModel symbol = Model.Symbols.FirstOrDefault(m => 
                        m.Id.Equals(marketSymbol.Model.Id, StringComparison.OrdinalIgnoreCase) &&
                        m.Market.Equals(marketSymbol.Model.Market, StringComparison.OrdinalIgnoreCase) &&
                        m.Security == marketSymbol.Model.Security);
                    if (symbol != null)
                    {
                        Symbols.Add(marketSymbol);
                    }
                }
            }
        }

        private void DoRemoveSymbols(IList symbols)
        {
            if (Symbols.Count == 0 || symbols.Count == 0)
                return;

            try
            {
                IsBusy = true;
                // Create a copy of the list before remove
                List<SymbolViewModel> list = symbols.Cast<SymbolViewModel>()?.ToList();
                Debug.Assert(list != null);

                int pos = Symbols.IndexOf(list.First());
                foreach (SymbolViewModel symbol in list)
                {
                    Symbols.Remove(symbol);
                }

                DataToModel();
                if (Symbols.Count > 0)
                {
                    SelectedSymbol = Symbols[Math.Min(pos, Symbols.Count - 1)];
                }

                MarketSymbols.View.Refresh();
                AddSymbolCommand.RaiseCanExecuteChanged();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoExportList()
        {
            try
            {
                IsBusy = true;
                DataToModel();
                SaveFileDialog saveFileDialog = new()
                {
                    InitialDirectory = Directory.GetCurrentDirectory(),
                    Filter = "symbol file (*.csv)|*.csv|All files (*.*)|*.*"
                };
                if (saveFileDialog.ShowDialog() == false)
                    return;

                try
                {
                    string fileName = saveFileDialog.FileName;
                    using StreamWriter file = File.CreateText(fileName);
                    foreach (SymbolViewModel symbol in Symbols)
                    {
                        file.WriteLine(symbol.Model.Id);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed reading {saveFileDialog.FileName}\n", true);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
