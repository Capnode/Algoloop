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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Algoloop.Model;
using Newtonsoft.Json.Linq;
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

        public async Task<IReadOnlyList<AccountModel>> GetAccountsAsync(Action<object> update)
        {
            // Skip if subscription active
            if (_fxcmSocket.AccountsUpdate != default && update != default) return null;

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

            _fxcmSocket.AccountsUpdate = update;
            return accounts;
        }

        public async Task<IReadOnlyList<SymbolModel>> GetSymbolsAsync(Action<object> update)
        {
            // { "response":{ "executed":false,"error":"Unauthorized"} }
            // Skip if subscription active
            if (_fxcmSocket.SymbolUpdate != default && update != default) return null;

            Log.Trace("{0}: GetSymbolsAsync", GetType().Name);
            string json = await GetAsync(_getModelOffer).ConfigureAwait(false);
            JObject jo = JObject.Parse(json);
            JToken jResponse = jo["response"];
            bool executed = (bool)jResponse["executed"];
            if (!executed)
            {
                string error = jResponse["error"].ToString();
                throw new ApplicationException(error);
            }

            var symbols = new List<SymbolModel>();
            JArray jOffers = JArray.FromObject(jo["offers"]);
            foreach (JToken jOffer in jOffers)
            {
                SymbolModel symbol = ToSymbol(jOffer);
                if (symbol == null) continue;
                symbols.Add(symbol);
            }

            _fxcmSocket.SymbolUpdate = update;
            return symbols;
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

        private static SymbolModel ToSymbol(JToken token)
        {
            string currency = token["currency"].ToString();
            if (string.IsNullOrWhiteSpace(currency)) return null;
            int instrumentType = (int)token["instrumentType"];
            var symbol = new SymbolModel
            {
                Id = currency,
                Name = currency,
                Market = Support.Market,
                Security = Support.ToSecurityType(instrumentType),
                Properties = new Dictionary<string, object>
                {
                    { "Ask", token["sell"].ToDecimal() },
                    { "Bid", token["buy"].ToDecimal() }
                }
            };

            return symbol;
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

        //private async Task<string> PostAsync(string path, string body)
        //{
        //    string uri = _httpClient.BaseAddress + path;
        //    using HttpResponseMessage response = await _httpClient.PostAsync(uri, new StringContent(body, Encoding.UTF8, _mediatype));
        //    if (!response.IsSuccessStatusCode)
        //    {
        //        string message = $"PostAsync fail {(int)response.StatusCode} ({response.ReasonPhrase})";
        //        Log.Error(message);
        //        throw new ApplicationException(message);
        //    }

        //    return await response.Content.ReadAsStringAsync();
        //}
    }
}
