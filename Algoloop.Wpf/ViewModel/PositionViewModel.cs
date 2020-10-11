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

using GalaSoft.MvvmLight;
using QuantConnect;
using System;

namespace Algoloop.Wpf.ViewModel
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

        public PositionViewModel(Holding holding)
        {
            Update(holding);
        }

        public string Symbol
        {
            get => _symbol;
            set => Set(ref _symbol, value);
        }

        public string SecurityType
        {
            get => _securityType;
            set => Set(ref _securityType, value);
        }

        public string CurrencySymbol
        {
            get => _symbolCurrency;
            set => Set(ref _symbolCurrency, value);
        }

        public decimal AveragePrice
        {
            get => _averagePrice;
            set => Set(ref _averagePrice, value);
        }

        public decimal Quantity
        {
            get => _quantity;
            set => Set(ref _quantity, value);
        }

        public decimal MarketPrice
        {
            get => _marketPrice;
            set => Set(ref _marketPrice, value);
        }

        public decimal ConversionRate
        {
            get => _conversionRate;
            set => Set(ref _conversionRate, value);
        }

        public decimal MarketValue
        {
            get => _marketValue;
            set => Set(ref _marketValue, value);
        }

        public decimal UnrealizedPnL
        {
            get => _unrealizedPnL;
            set => Set(ref _unrealizedPnL, value);
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
