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

using QuantConnect;
using QuantConnect.Orders;
using System;
using System.Diagnostics;

namespace Algoloop.Wpf.ViewModels
{
    public class HoldingViewModel : ViewModelBase
    {
        private DateTime _entryTime;
        private decimal _entryPrice;
        private decimal _quantity;
        private decimal _entryValue;

        public HoldingViewModel(Order order)
        {
            Symbol = order.Symbol;
            EntryTime = order.CreatedTime.ToLocalTime();
            EntryPrice = order.Price.SmartRounding();
            Quantity = order.Quantity;
            EntryValue = order.Quantity * order.Price;

            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public DateTime EntryTime
        {
            get => _entryTime;
            set => SetProperty(ref _entryTime, value);
        }

        public Symbol Symbol { get;}

        public decimal EntryPrice
        {
            get => _entryPrice;
            set => SetProperty(ref _entryPrice, value);
        }

        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public decimal EntryValue
        {
            get => _entryValue;
            set => SetProperty(ref _entryValue, value);
        }
    }
}
