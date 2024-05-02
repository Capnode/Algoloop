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
using QuantConnect;
using System;
using System.Diagnostics;

namespace Algoloop.Wpf.ViewModels
{
    public class PositionViewModel : ViewModelBase
    {
        private string _symbol;
        private string _securityType;
        private string _symbolCurrency;
        private decimal _averagePrice;
        private decimal _quantity;
        private decimal _marketPrice;
        private decimal _conversionRate;
        private decimal _marketValue;
        private decimal _unrealizedPnL;

        public PositionViewModel(PositionModel position)
        {
            Update(position);
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public PositionViewModel(Holding holding)
        {
            Update(holding);
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public string Symbol
        {
            get => _symbol;
            set => SetProperty(ref _symbol, value);
        }

        public string SecurityType
        {
            get => _securityType;
            set => SetProperty(ref _securityType, value);
        }

        public string CurrencySymbol
        {
            get => _symbolCurrency;
            set => SetProperty(ref _symbolCurrency, value);
        }

        public decimal AveragePrice
        {
            get => _averagePrice;
            set => SetProperty(ref _averagePrice, value);
        }

        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public decimal MarketPrice
        {
            get => _marketPrice;
            set => SetProperty(ref _marketPrice, value);
        }

        public decimal ConversionRate
        {
            get => _conversionRate;
            set => SetProperty(ref _conversionRate, value);
        }

        public decimal MarketValue
        {
            get => _marketValue;
            set => SetProperty(ref _marketValue, value);
        }

        public decimal UnrealizedPnL
        {
            get => _unrealizedPnL;
            set => SetProperty(ref _unrealizedPnL, value);
        }

        public void Update(PositionModel position)
        {
            if (position == null) throw new ArgumentNullException(nameof(position));

            Symbol = position.Symbol.Name;
            SecurityType = Enum.GetName(typeof(SecurityType), position.Symbol.Security);
            CurrencySymbol = position.PriceCurrency;
            AveragePrice = position.AveragePrice;
            Quantity = position.Quantity;
            MarketPrice = position.MarketPrice;
            ConversionRate = 1;
            MarketValue = position.MarketValue;
            UnrealizedPnL = position.MarketValue - position.EntryValue;
        }

        public void Update(Holding holding)
        {
            if (holding == null) throw new ArgumentNullException(nameof(holding));

            Symbol = holding.Symbol.ID.Symbol;
            SecurityType = Enum.GetName(typeof(SecurityType), holding.Type);
            CurrencySymbol = holding.CurrencySymbol;
            AveragePrice = holding.AveragePrice;
            Quantity = holding.Quantity;
            MarketPrice = holding.MarketPrice;
            ConversionRate = holding.ConversionRate ?? 0;
            MarketValue = holding.MarketValue;
            UnrealizedPnL = holding.UnrealizedPnL;
        }
    }
}
