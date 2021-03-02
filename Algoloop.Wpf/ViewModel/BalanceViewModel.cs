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
using QuantConnect.Securities;
using System;
using System.Diagnostics;

namespace Algoloop.Wpf.ViewModel
{
    public class BalanceViewModel : ViewModel
    {
        public BalanceViewModel(BalanceModel model)
        {
            Model = model;
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public BalanceViewModel(AccountEvent message)
        {
            Update(message);
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public BalanceModel Model { get; set; }

        public string Currency
        {
            get => Model.Currency;
            set
            {
                Model.Currency = value;
                RaisePropertyChanged(() => Currency);
            }
        }

        public decimal Cash
        {
            get => Model.Cash;
            set
            {
                Model.Cash = value;
                RaisePropertyChanged(() => Cash);
            }
        }

        public decimal Equity
        {
            get => Model.Equity;
            set
            {
                Model.Equity = value;
                RaisePropertyChanged(() => Equity);
            }
        }

        public void Update(CashAmount cash)
        {
            Model.Currency = cash.Currency;
            Model.Cash = cash.Amount;
        }

        public void Update(AccountEvent message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            Model.Currency = message.CurrencySymbol;
            Model.Cash = message.CashBalance;
        }
    }
}
