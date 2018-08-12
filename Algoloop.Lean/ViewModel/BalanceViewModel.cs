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
using QuantConnect.Securities;
using System;

namespace Algoloop.Lean.ViewModel
{
    public class BalanceViewModel : ViewModelBase
    {
        private string _securitySymbol;
        private string _conversionRateSecurity;
        private string _symbol;
        private decimal _amount;
        private decimal _conversionRate;
        private string _currencySymbol;
        private decimal _valueInAccountCurrency;

        public BalanceViewModel(Cash cash)
        {
            Update(cash);
        }

        //     Gets the symbol of the security required to provide conversion rates. If this
        //     cash represents the account currency, then QuantConnect.Symbol.Empty is returned
        public String SecuritySymbol
        {
            get => _securitySymbol;
            set => Set(ref _securitySymbol, value);
        }

        //     Gets the security used to apply conversion rates. If this cash represents the
        //     account currency, then null is returned.
        public String ConversionRateSecurity
        {
            get => _conversionRateSecurity;
            set => Set(ref _conversionRateSecurity, value);
        }

        //     Gets the symbol used to represent this cash
        public string Symbol
        {
            get => _symbol;
            set => Set(ref _symbol, value);
        }

        //     Gets or sets the amount of cash held
        public decimal Amount
        {
            get => _amount;
            set => Set(ref _amount, value);
        }

        //     Gets the conversion rate into account currency
        public decimal ConversionRate
        {
            get => _conversionRate;
            set => Set(ref _conversionRate, value);
        }

        //     The symbol of the currency, such as $
        public string CurrencySymbol
        {
            get => _currencySymbol;
            set => Set(ref _currencySymbol, value);
        }

        //     Gets the value of this cash in the account currency
        public decimal ValueInAccountCurrency
        {
            get => _valueInAccountCurrency;
            set => Set(ref _valueInAccountCurrency, value);
        }

        public void Update(Cash cash)
        {
            SecuritySymbol = cash.SecuritySymbol.ID.Symbol;
            ConversionRateSecurity = cash.ConversionRateSecurity?.Symbol.ID.Symbol;
            Symbol = cash.Symbol;
            Amount = cash.Amount;
            ConversionRate = cash.ConversionRate;
            CurrencySymbol = cash.CurrencySymbol;
            ValueInAccountCurrency = cash.ValueInAccountCurrency;
        }
    }
}
