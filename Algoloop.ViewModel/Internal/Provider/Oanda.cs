/*
 * Copyright 2023 Capnode AB
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
using NodaTime;
using QuantConnect;
using QuantConnect.Brokerages.Oanda;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Environment = QuantConnect.Brokerages.Oanda.Environment;

namespace Algoloop.ViewModel.Internal.Provider
{
    internal class Oanda : ProviderBase
    {
        private readonly DateTime _firstDate = new(2003, 05, 05);
        private readonly OandaSymbolMapper _symbolMapper = new();

        private OandaBrokerage _brokerage;
        private string _accountId;

        public override void Login(ProviderModel provider)
        {
            Log.Trace($">{GetType().Name}:Login {provider.Provider}");
            int uid = Config.GetInt("job-user-id", 0);
            string token = Config.Get("api-access-token", string.Empty);
            if (uid == 0 || string.IsNullOrEmpty(token)) throw new ApplicationException("A valid QuantConnect subscription is required for access");

            base.Login(provider);
            Environment environment = provider.Access == ProviderModel.AccessType.Real ?
                Environment.Trade : Environment.Practice;
            _accountId = provider.AccountId;
            _brokerage = new OandaBrokerage(null, null, null, environment, provider.ApiKey, provider.AccountId);
            _brokerage.Connect();
            Log.Trace($"<{GetType().Name}:Login {provider.Provider} Connected:{_brokerage.IsConnected}");
        }

        public override void Logout()
        {
            if (_brokerage == null) return;
            if(_brokerage.IsConnected)
            {
                _brokerage.Disconnect();
            }
        }

        public override void GetUpdate(ProviderModel market, Action<object> update, CancellationToken cancel)
        {
            if (market == default) throw new ArgumentNullException(nameof(market));
            List<Holding> holdings = _brokerage.GetAccountHoldings();
            List<CashAmount> cashAmounts = _brokerage.GetCashBalance();
            List<Order> orders = _brokerage.GetOpenOrders();

            // Get Accounts
            var account = new AccountModel(_accountId);
            cashAmounts.ForEach(m => account.Balances.Add(new BalanceModel(m)));
            holdings.ForEach(m => account.Positions.Add(new PositionModel(m)));
            orders.ForEach(m => account.Orders.Add(new OrderModel(m)));
            List<AccountModel> accounts = new() { account };
            UpdateAccounts(market, accounts, update);

            // Update symbols
            var tickers = OandaSymbolMapper.KnownTickers;
            var symbols = new List<SymbolModel>();
            foreach (string ticker in tickers)
            {
                SecurityType securityType = _symbolMapper.GetBrokerageSecurityType(ticker);
                Symbol symbol = _symbolMapper.GetLeanSymbol(ticker, securityType, Market.Oanda);
                if (!_symbolMapper.IsKnownLeanSymbol(symbol)) continue;
                SymbolModel symbolModel = market.Symbols.FirstOrDefault(m => m.Equals(symbol));
                if (symbolModel == default)
                {
                    symbolModel = new SymbolModel(symbol) { Active = false };
                }

                symbolModel.Id = ticker;
                symbolModel.Properties.Clear();
                symbols.Add(symbolModel);
            }

            // Exclude unknown symbols
            UpdateSymbols(market, symbols, update);

            // Download active symbols
            DateTime now = DateTime.UtcNow;
            foreach (SymbolModel symbol in market.Symbols.Where(m => m.Active))
            {
                Tick rate = _brokerage.GetRates(symbol.Id);
                rate.Symbol = symbol.LeanSymbol;
                update?.Invoke(rate);
            }

            market.LastDate = now.ToLocalTime();
        }

        //private void UpdateSymbols(ProviderModel market)
        //{
        //    Collection<SymbolModel> symbols = market.Symbols;

        //    // Get watchlists
        //    var all = new List<SymbolModel>();
        //    market.Lists.Clear();
        //    foreach (ListModel list in lists)
        //    {
        //        market.Lists.Add(list);
        //        foreach (SymbolModel symbol in list.Symbols)
        //        {
        //            if (!all.Contains(symbol))
        //            {
        //                all.Add(symbol);
        //            }
        //        }
        //    }

        //    bool addNew = symbols.Count != 1;  // Special testcase
        //    UpdateSymbols(market, all, addNew);
        //}

        private void UpdateHistory(ProviderModel market, Action<object> update)
        {
            if (market == null) throw new ArgumentNullException(nameof(market));
            IList<string> symbols = market.Symbols.Where(x => x.Active).Select(m => m.Id).ToList();
            var marketSymbol = market.Symbols.First();
            Symbol symbol = Symbol.Create(marketSymbol.Id, marketSymbol.Security, marketSymbol.Market);
            Resolution resolution = market.Resolution;
            DateTime lastDate = market.LastDate.ToUniversalTime();
            DateTime fromDate = lastDate < _firstDate ? _firstDate : lastDate;
            DateTime toDate = fromDate.AddDays(1).Date;
            string from = fromDate.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string to = toDate.AddTicks(-1).ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            Log.Trace($"{GetType().Name}: Download {market.Resolution} from={from} to={to}");
            DateTime now = DateTime.UtcNow;

            var historyProvider = new BrokerageHistoryProvider();
            historyProvider.SetBrokerage(_brokerage);
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, new DataPermissionManager(), null));
            var requests = new[]
            {
                new HistoryRequest(fromDate,
                    toDate,
                    typeof(QuoteBar),
                    symbol,
                    resolution,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.EasternStandard),
                    DateTimeZone.Utc,
                    Resolution.Minute,
                    false,
                    false,
                    DataNormalizationMode.Adjusted,
                    TickType.Quote)
            };

            IEnumerable<Slice> history = historyProvider.GetHistory(requests, TimeZones.Utc);
            foreach (var slice in history)
            {
                if (resolution == Resolution.Tick)
                {
                    foreach (var tick in slice.Ticks[symbol])
                    {
                        Log.Trace("{0}: {1} - {2} / {3}", tick.Time, tick.Symbol, tick.BidPrice, tick.AskPrice);
                    }
                }
                else
                {
                    var bar = slice.QuoteBars[symbol];

                    Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close);
                }
            }

            Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);

            if (toDate > now)
            {
                market.Active = false;
            }
            else
            {
                market.LastDate = toDate.ToLocalTime();
            }
        }
    }
}
