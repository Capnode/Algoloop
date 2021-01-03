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
using QuantConnect;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Statistics;
using System;
using System.Collections.Generic;

namespace Algoloop.Wpf.Provider
{
    public interface IProvider : IDisposable
    {
        void Register(SettingModel settings, string name);
        void Login(AccountModel account, SettingModel settings);
        void Logout();
        void Download(MarketModel market, SettingModel settings);
        void Abort();
        List<Order> GetOpenOrders();
        List<Holding> GetAccountHoldings();
        List<Trade> GetClosedTrades();
        List<CashAmount> GetCashBalance();
    }
}
