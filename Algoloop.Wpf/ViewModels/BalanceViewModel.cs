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

using Algoloop.Wpf.Model;
using QuantConnect.Securities;
using System;
using System.Diagnostics;

namespace Algoloop.Wpf.ViewModels
{
    public class BalanceViewModel : ViewModelBase
    {
        private decimal _cash;
        private decimal _equity;
        private decimal _profit;
        private decimal _dayProfit;
        private string _currency;

        public BalanceViewModel(BalanceModel model)
        {
            Update(model);
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public BalanceViewModel(AccountEvent message)
        {
            Update(message);
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public decimal Cash
        {
            get => _cash;
            set => SetProperty(ref _cash, value);
        }

        public decimal Equity
        {
            get => _equity;
            set => SetProperty(ref _equity, value);
        }


        public decimal Profit
        {
            get => _profit;
            set => SetProperty(ref _profit, value);
        }

        public decimal DayProfit
        {
            get => _dayProfit;
            set => SetProperty(ref _dayProfit, value);
        }

        public string Currency
        {
            get => _currency;
            set => SetProperty(ref _currency, value);
        }

        public void Update(BalanceModel balance)
        {
            Cash = balance.Cash;
            Equity = balance.Equity;
            Profit = balance.Profit;
            DayProfit = balance.DayProfit;
            Currency = balance.Currency;
        }

        public void Update(CashAmount cash)
        {
            Currency = cash.Currency;
            Cash = cash.Amount;
        }

        public void Update(AccountEvent message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            Currency = message.CurrencySymbol;
            Cash = message.CashBalance;
        }
    }
}
