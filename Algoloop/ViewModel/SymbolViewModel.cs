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
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.ComponentModel;

namespace Algoloop.ViewModel
{
    public class SymbolViewModel : MarketItemViewModel
    {
        private ViewModelBase _parent;

        public SymbolViewModel(ViewModelBase parent, SymbolModel model)
        {
            _parent = parent;
            Model = model;

            DeleteSymbolCommand = new RelayCommand(() => DeleteSymbol(this), true);
        }

        public SymbolModel Model { get; }

        [Browsable(false)]
        public RelayCommand DeleteSymbolCommand { get; }

        public bool Active
        {
            get => Model.Active;
            set
            {
                Model.Active = value;
                RaisePropertyChanged(() => Active);
                (_parent as StrategyViewModel)?.Refresh(this);
                (_parent as MarketViewModel)?.Refresh(this);
            }
        }

        private void DeleteSymbol(SymbolViewModel symbol)
        {
            (_parent as StrategyViewModel)?.DeleteSymbol(this);
            (_parent as MarketViewModel)?.DeleteSymbol(this);
        }
    }
}
