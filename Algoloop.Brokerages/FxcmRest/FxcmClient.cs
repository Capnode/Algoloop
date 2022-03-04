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
using System.Threading.Tasks;
using Algoloop.Brokerages.FxcmRest.Internal;
using Algoloop.Model;
using Newtonsoft.Json.Linq;
using QuantConnect;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using static Algoloop.Model.ProviderModel;

namespace Algoloop.Brokerages.FxcmRest
{
    /// <summary>
    /// https://github.com/fxcm/RestAPI
    /// </summary>
    public class FxcmClient : IDisposable
    {
        private const string _baseUrlReal = "https://api.fxcm.com";
        private const string _baseUrlDemo = "https://api-demo.fxcm.com";
        private const string _mediatype = @"application/json";
        private const string _getModel = @"trading/get_model/?models=OpenPosition&models=ClosedPosition" +
            "&models=Order&models=Account&models=LeverageProfile&models=Properties";
//        private const string _getInstruments = @"trading/get_instruments";
        private const string _getModelOffer = @"trading/get_model/?models=Offer";
        private const string _getSymbols = @"trading/get_instruments";
        private const string _subscribe = @"subscribe";
        private const string _unsubscribe = @"unsubscribe";

        private bool _isDisposed;
        private string _baseCurrency;
        private readonly string _baseUrl;
        private readonly string _key;
        private readonly FxcmSocket _fxcmSocket;
        private readonly HttpClient _httpClient;

        public FxcmClient(ProviderModel.AccessType access, string key)
        {
            _key = key;
            _baseUrl = access.Equals(AccessType.Demo) ? _baseUrlDemo : _baseUrlReal;
            var uri = new Uri(_baseUrl);
            _fxcmSocket = new FxcmSocket(uri, key);
            _httpClient = new HttpClient { BaseAddress = uri };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_mediatype));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "request");
        }
            
        public void Login()
        {
            _fxcmSocket.Connect();
            string bearer = _fxcmSocket.Sid + _key;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        public void Logout()
        {
            _fxcmSocket.Close();
        }

        public async Task<IReadOnlyList<SymbolModel>> GetSymbolsAsync()
        {
            // {"response":{"executed":true},"data":{"instrument":[{"symbol":"EUR/USD","visible":true,"order":100},
            Log.Trace("{0}: GetSymbolsAsync", GetType().Name);
            string json = await GetAsync(_getSymbols).ConfigureAwait(false);
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
                bool visible = (bool)jInstrument["visible"];
                int order = (int)jInstrument["order"];
                var symbol = new SymbolModel
                {
                    Active = visible,
                    Id = order.ToString(),
                    Name = name,
                    Market = Support.Market,
                    Security = Support.ToSecurityType(0),
                    Properties = new Dictionary<string, object>()
                };

                symbols.Add(symbol);
            }

            return symbols;
        }

        public async Task GetAccountsAsync(Action<object> update)
        {
            // Skip if subscription active
            if (_fxcmSocket.AccountsUpdate != default && update != default) return;

            Log.Trace("{0}: GetAccountsAsync", GetType().Name);
            string json = await GetAsync(_getModel).ConfigureAwait(false);
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

            update(accounts);
            _fxcmSocket.AccountsUpdate = update;
        }

        public async Task GetMarketDataAsync(Action<object> update)
        {
// {"response":{ "executed":true},"offers":[{ "t":0,"ratePrecision":5,"offerId":1,"rollB":-0.7542,"rollS":0.3162,"fractionDigits":5,"pip":0.0001,"defaultSortOrder":100,"currency":"EUR/USD","instrumentType":1,"valueDate":"08062021","time":"2021-08-04T17:00:00.347Z","sell":1.18374,"buy":1.18385,"sellTradable":true,"buyTradable":true,"high":1.19001,"low":1.18329,"volume":1,"pipFraction":0.1,"spread":1.1,"mmr":33.3,"emr":33.3,"lmr":16.65,"minQuantity":1,"maxQuantity":50000000,"instrBaseUnitSize":1,"pipCost":0.08447}
            Log.Trace("{0}: GetOffersAsync", GetType().Name);
            string json = await GetAsync(_getModelOffer).ConfigureAwait(false);
            JObject jo = JObject.Parse(json);
            JToken jResponse = jo["response"];
            bool executed = (bool)jResponse["executed"];
            if (!executed)
            {
                string error = jResponse["error"].ToString();
                throw new ApplicationException(error);
            }

            var quoteBars = new List<QuoteBar>();
            JArray jOffers = JArray.FromObject(jo["offers"]);
            foreach (JToken jOffer in jOffers)
            {
                string ticker = jOffer["currency"].ToString();
                if (string.IsNullOrWhiteSpace(ticker)) continue;
                int instrumentType = (int)jOffer["instrumentType"];
                SecurityType security = Support.ToSecurityType(instrumentType);
                Symbol symbol = Symbol.Create(ticker, security, Support.Market);
                DateTime utcTime = DateTime.Parse(jOffer["time"].ToString());
                decimal bid = jOffer["sell"].ToDecimal();
                decimal ask = jOffer["buy"].ToDecimal();
                var bidBar = new Bar(0, 0, 0, bid);
                var askBar = new Bar(0, 0, 0, ask);
                var quoteBar = new QuoteBar(utcTime, symbol, bidBar, 0, askBar, 0);
                quoteBars.Add(quoteBar);
            }

            update(quoteBars);
        }

        public async Task SubscribeMarketDataAsync(IEnumerable<SymbolModel> symbols, Action<object> update)
        {
            Log.Trace("{0}: SubscribeMarketDataAsync", GetType().Name);

            List<KeyValuePair<string, string>> nvc = symbols
                .Where(m => m.Active)
                .Select(n => new KeyValuePair<string, string>("pairs", n.Name)).ToList();
            string json = await PostAsync(_subscribe, nvc).ConfigureAwait(false);
//  { "response":{ "executed":true,"error":""},"pairs":[{ "Updated":1628025032910,"Rates":[1.18608,1.18646,1.18706,1.18578,1.18608,1.18646],"Symbol":"EUR/USD"}]}
            JObject jo = JObject.Parse(json);
            JToken jResponse = jo["response"];
            bool executed = (bool)jResponse["executed"];
            if (!executed)
            {
                string error = jResponse["error"].ToString();
                throw new ApplicationException(error);
            }
            var quoteBars = new List<QuoteBar>();
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
                var bidBar = new Bar(0, 0, 0, bid);
                var askBar = new Bar(0, 0, 0, ask);
                SymbolModel symbolModel = symbols.FirstOrDefault(m => m.Name.Equals(ticker));
                if (symbolModel == default) continue;
                Symbol symbol = Symbol.Create(ticker, symbolModel.Security, symbolModel.Market);
                var quoteBar = new QuoteBar(utcTime, symbol, bidBar, 0, askBar, 0);
                quoteBars.Add(quoteBar);
            }

            update(quoteBars);
            _fxcmSocket.SymbolUpdate = update;
        }

        public async Task UnsubscribeQuotesAsync(IEnumerable<SymbolModel> symbols)
        {
            Log.Trace("{0}: UnsubscribeMarketDataAsync", GetType().Name);

            List<KeyValuePair<string, string>> nvc = symbols
                .Where(m => m.Active)
                .Select(n => new KeyValuePair<string, string>("pairs", n.Name)).ToList();
            string json = await PostAsync(_unsubscribe, nvc).ConfigureAwait(false);
            // { {"response":{"executed":true,"error":""},"pairs":["EUR/USD"]}
            JObject jo = JObject.Parse(json);
            JToken jResponse = jo["response"];
            bool executed = (bool)jResponse["executed"];
            if (!executed)
            {
                string error = jResponse["error"].ToString();
                throw new ApplicationException(error);
            }
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

        private AccountModel ToAccount(JToken token)
        {
            if (token["isTotal"] != null) return null;

            var account = new AccountModel
            {
                Id = token["accountId"].ToString(),
                Name = token["accountName"].ToString(),
            };

            var balance = new BalanceModel
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
                Market = Support.Market,
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

        private async Task<string> PostAsync(string path, IList<KeyValuePair<string, string>> nvc)
        {
            string uri = _httpClient.BaseAddress + path;
            using HttpResponseMessage response = await _httpClient.PostAsync(uri, new FormUrlEncodedContent(nvc));
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
