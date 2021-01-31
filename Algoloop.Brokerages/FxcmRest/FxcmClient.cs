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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Algoloop.Model;
using Newtonsoft.Json.Linq;
using QuantConnect.Logging;
using WebSocketSharp;
using static Algoloop.Model.ProviderModel;

namespace Algoloop.Brokerages.FxcmRest
{
    /// <summary>
    /// https://github.com/fxcm/RestAPI
    /// </summary>
    public class FxcmClient : IDisposable
    {
        private const string _market = "fxcm-rest";
        private const string _baseUrlReal = "https://api.fxcm.com";
        private const string _baseUrlDemo = "https://api-demo.fxcm.com";
        private const string _mediatype = @"application/json";
        private const string _getModel = @"trading/get_model/?models=OpenPosition&models=ClosedPosition" +
            "&models=Order&models=Account&models=LeverageProfile&models=Properties";

        private bool _isDisposed;
        private readonly string _baseUrl;
        private readonly string _key;
        private readonly HttpClient _httpClient;
        private readonly WebSocket _webSocket;
        private ManualResetEvent _hold = new ManualResetEvent(false);

        public FxcmClient(ProviderModel.AccessType access, string key)
        {
            _baseUrl = access.Equals(AccessType.Demo) ? _baseUrlDemo : _baseUrlReal;
            _key = key;

            var uri = new Uri(_baseUrl);
            string url = $"wss://{uri.Host}:{uri.Port}/socket.io/?EIO=3&transport=websocket&access_token={_key}";
            _webSocket = new WebSocket(url);
            _webSocket.OnOpen += _webSocket_OnOpen;
            _webSocket.OnMessage += _webSocket_OnMessage;
            _webSocket.OnError += _webSocket_OnError;
            _webSocket.OnClose += _webSocket_OnClose;
            _webSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;

            _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_mediatype));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "request");
        }

        public bool LoginAsync()
        {
            _webSocket.Connect();
            if (!_hold.WaitOne(TimeSpan.FromSeconds(20))) return false;
            if (!_webSocket.IsAlive) return false;
            return true;
        }

        private void _webSocket_OnOpen(object sender, EventArgs e)
        {
            Log.Trace("OnOpen");
        }

        private void _webSocket_OnClose(object sender, CloseEventArgs e)
        {
            Log.Trace("OnClose");
        }

        private void _webSocket_OnError(object sender, ErrorEventArgs e)
        {
            Log.Error(e.Exception);
            _hold.Set();
        }

        private void _webSocket_OnMessage(object sender, MessageEventArgs e)
        {
            Log.Trace(e.Data);
            if (e.IsText)
            {
                if (e.Data.StartsWith("0", StringComparison.OrdinalIgnoreCase))
                {
                    JObject jo = JObject.Parse(e.Data.TrimStart('0'));
                    string sid = jo["sid"].ToString();
                    string bearer = sid + _key;
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                }
                else if (e.Data.StartsWith("40", StringComparison.OrdinalIgnoreCase))
                {
                    _hold.Set();
                }
            }
        }

        public bool LogoutAsync()
        {
            _webSocket.Close();
            return true;
        }

        public async Task<IReadOnlyList<AccountModel>> GetAccountsAsync()
        {
            Log.Trace("{0}: GetAccountsAsync", GetType().Name);
            string json = await GetAsync(_getModel);
            var accounts = new List<AccountModel>();
            JObject jo = JObject.Parse(json);
            JArray jAccounts = JArray.FromObject(jo["accounts"]);
            foreach (JToken jAccount in jAccounts)
            {
                var account = new AccountModel
                {
                    Id = jAccount["accountId"].ToString(),
                    Name = jAccount["accountName"].ToString(),
                };

                accounts.Add(account);
            }

            return accounts;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }

                _isDisposed = true;
            }
        }

        private async Task<string> GetAsync(string path)
        {
            string uri = _httpClient.BaseAddress + path;
            using (HttpResponseMessage response = await _httpClient.GetAsync(uri))
            {
                if (!response.IsSuccessStatusCode)
                {
                    string message = $"GetAsync fail {(int)response.StatusCode} ({response.ReasonPhrase})";
                    Log.Error(message);
                    throw new ApplicationException(message);
                }

                return await response.Content.ReadAsStringAsync();
            }
        }

        private async Task<string> PostAsync(string path, string body)
        {
            string uri = _httpClient.BaseAddress + path;
            using (HttpResponseMessage response = await _httpClient.PostAsync(uri, new StringContent(body, Encoding.UTF8, _mediatype)))
            {
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
}
