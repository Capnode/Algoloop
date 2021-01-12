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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Logging;
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

        private bool _isDisposed;
        private readonly string _baseUrl;
        private readonly string _key;
//        private readonly SocketIO _socketio;
        private readonly HttpClient _httpClient;

        public FxcmClient(string access, string key)
        {
            _baseUrl = access.Equals(nameof(AccessType.Demo), StringComparison.OrdinalIgnoreCase)
                ? _baseUrlDemo : _baseUrlReal;
            _key = key;

            //_socketio = new SocketIO(_baseUrl + $"/?access_token={_key}");
            //_socketio.OnConnected += OnConnected;
            //_socketio.OnDisconnected += OnDisconnected;
            //_socketio.OnError += OnError;

            _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_mediatype));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "request");
        }

        private void OnConnected(object sender, EventArgs e)
        {
//            Log.Trace($"Websocket connected:{_socketio.Id}");
        }

        private void OnDisconnected(object sender, string e)
        {
        }

        private void OnError(object sender, string e)
        {
        }

        public async Task LoginAsync()
        {
            //            await _socketio.ConnectAsync();
            await Task.Delay(10);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _key);
        }

        public Task LogoutAsync()
        {
            return Task.CompletedTask;
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
