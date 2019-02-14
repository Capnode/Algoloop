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
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Data;

namespace Algoloop.ViewModel
{
    public class FolderViewModel : ViewModelBase
    {
        private MarketViewModel _market;

        public FolderViewModel(MarketViewModel market, FolderModel model)
        {
            _market = market;
            Model = model;

            DeleteCommand = new RelayCommand(() => _market?.DeleteFolder(this), () => !_market.Active);
            DataFromModel();

            ActiveSymbols.Filter += (object sender, FilterEventArgs e) =>
            {
                SymbolViewModel symbol = e.Item as SymbolViewModel;
                if (symbol != null)
                {
                    e.Accepted = symbol.Model.Active;
                }
            };

            ActiveSymbols.Source = Symbols;
        }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();
        public CollectionViewSource ActiveSymbols { get; } = new CollectionViewSource();
        public FolderModel Model { get; }
        public RelayCommand DeleteCommand { get; }

        internal void Refresh(SymbolViewModel symbol)
        {
            DataFromModel();
            ActiveSymbols.View.Refresh();
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
            foreach (SymbolViewModel marketSymbol in _market.Symbols)
            {
                if (marketSymbol.Active)
                {
                    SymbolModel symbol = Model.Symbols.Find(m => m.Name.Equals(marketSymbol.Model.Name));
                    if (symbol == null)
                    {
                        symbol = new SymbolModel(marketSymbol.Model) { Active = false };
                    }
                    var folderSymbol = new SymbolViewModel(this, symbol);
                    Symbols.Add(folderSymbol);
                }
            }
        }
    }
}
