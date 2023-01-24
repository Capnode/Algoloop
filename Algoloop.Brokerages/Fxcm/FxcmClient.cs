/*
 * Copyright 2021 Capnode AB
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Algoloop.Brokerages.Fxcm.Internal;
using Algoloop.Model;
using Newtonsoft.Json.Linq;
using QuantConnect;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using static Algoloop.Model.ProviderModel;

namespace Algoloop.Brokerages.Fxcm
{
    /// <summary>
    /// https://fxcm-api.readthedocs.io/en/latest/restapi.html
    /// https://github.com/fxcm/RestAPI
    /// Apply for a demo account Generate access token. You can generate one from the Trading Station web.
    /// Click on User Account > Token Management on the upper right hand of the website. For Live account,
    /// please send your username to api@fxcm.com, we will need to enable Rest API access. For demo account,
    /// Rest API access was enabled by default.
    /// 
    /// TradingStation Web: https://tradingstation.fxcm.com
    /// </summary>
    public class FxcmClient : IDisposable
    {
        private const string BaseUrlReal = "https://api.fxcm.com";
        private const string BaseUrlDemo = "https://api-demo.fxcm.com";
        private const string Mediatype = @"application/json";
        private const string GetTables = @"trading/get_model/?models=Offer&models=OpenPosition&models=ClosedPosition" +
            "&models=Order&models=Account&models=LeverageProfile&models=Properties";
        private const string SubscribeTables = @"trading/subscribe";
        private const string UnsubscribeTables = @"trading/unsubscribe";
        private const string GetSymbols = @"trading/get_instruments";
        private const string Update_subscriptions = @"trading/update_subscriptions";
        private const string Subscribe = @"subscribe";
        private const string Unsubscribe = @"unsubscribe";
        private const string ContentType = "application/x-www-form-urlencoded";
        private readonly string[] _tables = { "Offer", "OpenPosition", "ClosedPosition", "Order", "Account", "Summary" };

        private bool _isDisposed;
        private string _baseCurrency;
        private readonly string _baseUrl;
        private readonly string _key;
        private readonly FxcmSocket _fxcmSocket;
        private readonly HttpClient _httpClient;

        public FxcmClient(ProviderModel.AccessType access, string key)
        {
            _key = key;
            _baseUrl = access.Equals(AccessType.Demo) ? BaseUrlDemo : BaseUrlReal;
            var uri = new Uri(_baseUrl);
            _fxcmSocket = new FxcmSocket(uri, key);
            _httpClient = new HttpClient { BaseAddress = uri };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Mediatype));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "request");
        }
            
        public void Login()
        {
            _fxcmSocket.Connect();
            string bearer = _fxcmSocket.Sid + _key;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        public async Task Logout()
        {
            // Stop subscription
            if (_fxcmSocket.Update != default)
            {
                _fxcmSocket.Update = default;
                IList<string> lines = _tables
                     .Select(table => $"models={table}")
                     .ToList();
                string json = await PostAsync(UnsubscribeTables, lines).ConfigureAwait(false);
                JObject jo = JObject.Parse(json);
                JToken jResponse = jo["response"];
                bool executed = (bool)jResponse["executed"];
                if (!executed)
                {
                    string error = jResponse["error"].ToString();
                    throw new ApplicationException(error);
                }
            }

            _fxcmSocket.Close();
        }

        public async Task GetAccountsAsync(Action<object> update)
        {
            //Log.Trace(">{0}:GetAccountsAsync", GetType().Name);
            string json = await GetAsync(GetTables).ConfigureAwait(false);
            JObject jo = JObject.Parse(json);
            JToken jResponse = jo["response"];
            bool executed = (bool)jResponse["executed"];
            if (!executed)
            {
                string error = jResponse["error"].ToString();
                throw new ApplicationException(error);
            }

            // Properties
            JArray jProperties = JArray.FromObject(jo["properties"]);
            foreach (JToken jProperty in jProperties)
            {
                ReadProperty(jProperty);
            }

            // Offers
            List<SymbolModel> symbols = new();
            List<QuoteBar> quotes = new();
            JArray jOffers = JArray.FromObject(jo["offers"]);
            foreach (JToken jSymbol in jOffers)
            {
                SymbolModel symbol = ToSymbolModel(jSymbol);
                if (symbol == null) continue;
                symbols.Add(symbol);
                QuoteBar quote = ToQuoteBar(jSymbol);
                quotes.Add(quote);
            }
            if (update != default)
            {
                update(symbols);
                update(quotes);
            }

            // Accounts
            var accounts = new List<AccountModel>();
            JArray jAccounts = JArray.FromObject(jo["accounts"]);
            foreach (JToken jAccount in jAccounts)
            {
                AccountModel account = ToAccount(jAccount);
                if (account == null) continue;
                accounts.Add(account);
            }

            // Orders
            JArray jOrders = JArray.FromObject(jo["orders"]);
            foreach (JToken jOrder in jOrders)
            {
                // Find account
                string accountId = jOrder["accountId"].ToString();
                AccountModel account = accounts.Find(m => m.Id.Equals(accountId));
                if (account == default) continue;

                OrderModel order = ToOrder(jOrder);
                if (order == default) continue;
                account.Orders.Add(order);
            }

            // Open positions
            JArray jPositions = JArray.FromObject(jo["open_positions"]);
            foreach (JToken jPosition in jPositions)
            {
                // Find account
                string accountId = jPosition["accountId"].ToString();
                AccountModel account = accounts.Find(m => m.Id.Equals(accountId));
                if (account == default) continue;

                PositionModel position = ToPosition(jPosition);
                if (position == null) continue;
                account.Positions.Add(position);
            }

            if (update != default)
            {
                update(accounts);
            }

            // Start subscription
            IList<string> lines = _tables
                 .Select(table => $"models={table}")
                 .ToList();
            json = await PostAsync(SubscribeTables, lines).ConfigureAwait(false);
            jo = JObject.Parse(json);
            jResponse = jo["response"];
            executed = (bool)jResponse["executed"];
            if (!executed)
            {
                string error = jResponse["error"].ToString();
                throw new ApplicationException(error);
            }

            _fxcmSocket.Update = update;
            //Log.Trace("<{0}:GetAccountsAsync", GetType().Name);
        }

        public async Task<IReadOnlyList<SymbolModel>> GetSymbolsAsync()
        {
            // {"response":{"executed":true},"data":{"instrument":[{"symbol":"EUR/USD","visible":true,"order":100},
            //Log.Trace(">{0}:GetSymbolsAsync", GetType().Name);
            string json = await GetAsync(GetSymbols).ConfigureAwait(false);
            JObject jo = JObject.Parse(json);
            JToken jResponse = jo["response"];
            bool executed = (bool)jResponse["executed"];
            if (!executed)
            {
                string error = jResponse["error"].ToString();
                throw new ApplicationException(error);
            }

            var symbols = new List<SymbolModel>();
            JToken jData = jo["data"];
            JArray jInstruments = JArray.FromObject(jData["instrument"]);
            foreach (JToken jInstrument in jInstruments)
            {
                string name = jInstrument["symbol"].ToString();
                if (string.IsNullOrEmpty(name)) continue;
                bool visible = (bool)jInstrument["visible"];
                int order = (int)jInstrument["order"];
                var symbol = new SymbolModel
                {
                    Active = visible,
                    Id = order.ToString(),
                    Name = name,
                    Market = Market.FXCM,
                    Security = Support.ToSecurityType(0),
                };

                symbols.Add(symbol);
            }

            //Log.Trace("<{0}:GetSymbolsAsync", GetType().Name);
            return symbols;
        }

        public async Task SubscribeOfferAsync(SymbolModel symbol)
        {
            //Log.Trace(">{0}:SubscribeSymbolsAsync", GetType().Name);
            IList<string> lines = new List<string>() { $"symbol={symbol.Name}&visible={symbol.Active.ToString().ToLowerInvariant()}" };
            string json = await PostAsync(Update_subscriptions, lines).ConfigureAwait(false);
            JObject jo = JObject.Parse(json);
            JToken jResponse = jo["response"];
            bool executed = (bool)jResponse["executed"];
            if (!executed)
            {
                string error = jResponse["error"].ToString();
                throw new ApplicationException(error);
            }
            //Log.Trace("<{0}:SubscribeSymbolsAsync", GetType().Name);
        }

        public async Task SubscribeMarketDataAsync(IEnumerable<SymbolModel> symbols, Action<object> update)
        {
            //Log.Trace(">{0}:SubscribeMarketDataAsync", GetType().Name);
            IList<string> lines = symbols
                .Where(m => m.Active)
                .Select(n => $"pairs={n.Name}")
                .ToList();
            string json = await PostAsync(Subscribe, lines).ConfigureAwait(false);
//  { "response":{ "executed":true,"error":""},"pairs":[{ "Updated":1628025032910,"Rates":[1.18608,1.18646,1.18706,1.18578,1.18608,1.18646],"Symbol":"EUR/USD"}]}
            JObject jo = JObject.Parse(json);
            JToken jResponse = jo["response"];
            bool executed = (bool)jResponse["executed"];
            if (!executed)
            {
                string error = jResponse["error"].ToString();
                throw new ApplicationException(error);
            }
            List<QuoteBar> quoteBars = new ();
            JArray jPairs = JArray.FromObject(jo["pairs"]);
            foreach (JToken jPair in jPairs)
            {
                long updated = (long)jPair["Updated"];
                DateTime utcTime = Support.ToTime(updated);
                string ticker = jPair["Symbol"].ToString();
                JArray jRates = JArray.FromObject(jPair["Rates"]);
                decimal bid = jRates[0].ToDecimal();
                decimal ask = jRates[1].ToDecimal();
                decimal high = jRates[2].ToDecimal();
                decimal low = jRates[3].ToDecimal();
                Bar bidBar = new (0, 0, 0, bid);
                Bar askBar = new (0, 0, 0, ask);
                SymbolModel symbolModel = symbols.FirstOrDefault(m => m.Name.Equals(ticker));
                if (symbolModel == default) continue;
                Symbol symbol = Symbol.Create(ticker, symbolModel.Security, symbolModel.Market);
                var quoteBar = new QuoteBar(utcTime, symbol, bidBar, 0, askBar, 0);
                quoteBars.Add(quoteBar);
            }

            if (update != default)
            {
                update(quoteBars);
            }

            _fxcmSocket.Update = update;
            //Log.Trace("<{0}:SubscribeMarketDataAsync", GetType().Name);
        }

        public async Task UnsubscribeMarketData(IEnumerable<SymbolModel> symbols)
        {
            //Log.Trace("{0}:>UnsubscribeMarketDataAsync", GetType().Name);
            IList<string> lines = symbols
                .Where(m => m.Active)
                .Select(n => $"pairs={n.Name}")
                .ToList();
            string json = await PostAsync(Unsubscribe, lines).ConfigureAwait(false);
            // { {"response":{"executed":true,"error":""},"pairs":["EUR/USD"]}
            JObject jo = JObject.Parse(json);
            JToken jResponse = jo["response"];
            bool executed = (bool)jResponse["executed"];
            if (!executed)
            {
                string error = jResponse["error"].ToString();
                throw new ApplicationException(error);
            }
            //Log.Trace("{0}:<UnsubscribeMarketDataAsync", GetType().Name);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            _isDisposed = true;
            if (disposing)
            {
                _fxcmSocket.Dispose();
                _httpClient.Dispose();
            }
        }

        private void ReadProperty(JToken token)
        {
            string name = token["name"].ToString();
            string value = token["value"].ToString();
            switch (name)
            {
                case "BASE_CRNCY":
                    _baseCurrency = value;
                    break;
            }
        }

        private static SymbolModel ToSymbolModel(JToken token)
        {
            string ticker = token["currency"].ToString();
            if (string.IsNullOrWhiteSpace(ticker)) return null;
            int instrumentType = (int)token["instrumentType"];
            SecurityType security = Support.ToSecurityType(instrumentType);
            if (security.Equals(SecurityType.Base)) return null;

            SymbolModel symbol = new()
            {
                Active = true,
                Id = token["defaultSortOrder"].ToString(),
                Name = ticker,
                Market = Market.FXCM,
                Security = security
            };
            return symbol;
        }

        private static QuoteBar ToQuoteBar(JToken token)
        {
            string ticker = token["currency"].ToString();
            if (string.IsNullOrWhiteSpace(ticker)) return null;
            int instrumentType = (int)token["instrumentType"];
            SecurityType security = Support.ToSecurityType(instrumentType);
            Symbol symbol = Symbol.Create(ticker, security, Market.FXCM);
            DateTime utcTime = DateTime.Parse(token["time"].ToString());
            decimal bid = token["sell"].ToDecimal();
            decimal ask = token["buy"].ToDecimal();
            Bar bidBar = new (0, 0, 0, bid);
            Bar askBar = new (0, 0, 0, ask);
            QuoteBar quoteBar = new (utcTime, symbol, bidBar, 0, askBar, 0);
            return quoteBar;
        }

        private AccountModel ToAccount(JToken token)
        {
            if (token["isTotal"] != null) return null;

            AccountModel account = new ()
            {
                Id = token["accountId"].ToString(),
                Name = token["accountName"].ToString(),
            };

            BalanceModel balance = new ()
            {
                Currency = _baseCurrency,
                Cash = token["balance"].ToDecimal(),
                Equity = token["equity"].ToDecimal(),
                Profit = token["grossPL"].ToDecimal(),
                DayProfit = token["dayPL"].ToDecimal(),
            };

            account.Balances.Add(balance);
            return account;
        }

        private static OrderModel ToOrder(JToken token)
        {
            if (token["isTotal"] != null) return null;

            return new OrderModel
            {
                BrokerId = new Collection<string> { token["orderId"].ToString() },
                LimitPrice = token["limit"].ToDecimal(),
                Quantity = token["amountK"].ToDecimal()
            };
        }

        private static PositionModel ToPosition(JToken token)
        {
            // Skip summary row
            if (token["isTotal"] != null) return default;

            bool isBuy = (bool)token["isBuy"];
            decimal amountK = token["amountK"].ToDecimal();
            decimal open = token["open"].ToDecimal();
            decimal close = token["close"].ToDecimal();
            string currency = token["currency"].ToString();
            var symbol = new SymbolModel
            {
                Id = currency,
                Name = currency,
                Market = Market.FXCM,
                Security = Support.ToSecurityType(currency),
            };
            string entryTime = token["time"].ToString();
//            decimal grossPl = token["grossPL"].ToDecimal();
            decimal entryValue = amountK * open;
            decimal marketValue = amountK * close;
            var position = new PositionModel
            {
                Symbol = symbol,
                AveragePrice = open,
                Quantity = isBuy ? amountK : -amountK,
                MarketPrice = close,
                EntryValue = entryValue,
                MarketValue = marketValue,
                EntryTime = Support.ToTime(entryTime),
                UpdateTime = DateTime.Now
            };

            return position;
        }

        private async Task<string> GetAsync(string path)
        {
            if (!_fxcmSocket.IsAlive) throw new ApplicationException("Socket.IO closed");

            Log.Trace(_httpClient.DefaultRequestHeaders.Authorization.ToString());

            string uri = _httpClient.BaseAddress + path;
            using HttpResponseMessage response = await _httpClient.GetAsync(uri).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string message = $"GetAsync fail {(int)response.StatusCode} ({response.ReasonPhrase})";
                Log.Error(message);
                throw new ApplicationException(message);
            }

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private async Task<string> PostAsync(string path, IList<string> lines)
        {
            string uri = _httpClient.BaseAddress + path;
            string line = string.Join("\r\n", lines).Replace("/", "%2F");
            StringContent content = new (line, Encoding.UTF8, ContentType);
            using HttpResponseMessage response = await _httpClient.PostAsync(uri, content);
            if (!response.IsSuccessStatusCode)
            {
                string message = $"PostAsync fail {(int)response.StatusCode} ({response.ReasonPhrase})";
                Log.Error(message);
                throw new ApplicationException(message);
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
