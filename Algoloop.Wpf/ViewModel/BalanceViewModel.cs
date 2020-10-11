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

namespace Algoloop.Wpf.ViewModel
{
    public class BalanceViewModel : ViewModelBase
    {
        private string _currency;
        private decimal _amount;

        public BalanceViewModel(CashAmount cash)
        {
            Update(cash);
        }

        public BalanceViewModel(AccountEvent message)
        {
            Update(message);
        }

        // Gets the symbol of the security required to provide conversion rates. If this
        // cash represents the account currency, then QuantConnect.Symbol.Empty is returned
        public string Currency
        {
            get => _currency;
            set => Set(ref _currency, value);
        }

        //     Gets or sets the amount of cash held
        public decimal Amount
        {
            get => _amount;
            set => Set(ref _amount, value);
        }

        public void Update(CashAmount cash)
        {
            Currency = cash.Currency;
            Amount = cash.Amount;
        }

        public void Update(AccountEvent message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            Currency = message.CurrencySymbol;
            Amount = message.CashBalance;
        }
    }
}
