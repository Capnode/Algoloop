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

using Newtonsoft.Json.Linq;
using QuantConnect;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using SocketIOClient;
using SocketIOClient.EventArguments;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Algoloop.Brokerages.FxcmRest
{
    public class FxcmSocket2 : IDisposable
    {
        private readonly SocketIO _socketio;
        private readonly ManualResetEvent _hold = new(false);

        internal Action<object> AccountsUpdate { get; set; }
        internal Action<object> SymbolUpdate { get; set; }

        public string Sid { get; private set; }
        public bool IsAlive => _socketio.Connected;

        public FxcmSocket2(Uri uri, string key)
        {
            var options = new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string> { { "access_token", key } }
            };
            _socketio = new SocketIO(uri, options);
            _socketio.OnConnected += OnConnected;
            _socketio.OnDisconnected += OnDisconnected;
            _socketio.OnError += OnError;
            _socketio.OnReceivedEvent += OnReceivedEvent;
        }

        public void Dispose()
        {
            _hold.Dispose();
            GC.SuppressFinalize(this);
        }

        private void OnReceivedEvent(object sender, ReceivedEventArgs e)
        {
            Log.Trace(e.Event);
            MessageEvent(e.Response.ToString());
        }

        public void Connect()
        {
            _socketio.ConnectAsync().Wait();
            if (!_hold.WaitOne(TimeSpan.FromSeconds(20))) throw new ApplicationException($"{GetType().Name} Failed to login");
            Sid = _socketio.Id;
        }

        public void Close()
        {
            try
            {
                _socketio.DisconnectAsync().Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void OnConnected(object sender, EventArgs e)
        {
            Log.Trace("OnConnected");
            _hold.Set();
        }

        private void OnDisconnected(object sender, string e)
        {
            Log.Trace("OnDisconnected");
        }

        private void OnError(object sender, string e)
        {
            Log.Error(e);
            _hold.Set();
        }

        private void MessageEvent(string json)
        {
            if (SymbolUpdate == default) return;

            JArray jArray = JArray.Parse(json);
            string ticker = jArray[0].ToString();
            json = jArray[1].ToString();
            JObject jo = JObject.Parse(json);
            long updated = (long)jo["Updated"];
            DateTime utcTime = Support.ToTime(updated);
            string ticker2 = jo["Symbol"].ToString();
            string rates = jo["Rates"].ToString();
            JArray jRates = JArray.Parse(rates);
            decimal bid = jRates[0].ToDecimal();
            decimal ask = jRates[1].ToDecimal();
            decimal high = jRates[2].ToDecimal();
            decimal low = jRates[3].ToDecimal();
            var bidBar = new Bar(0, 0, 0, bid);
            var askBar = new Bar(0, 0, 0, ask);
            var symbol = Symbol.Create(ticker, SecurityType.Forex, Support.Market);
            var quoteBar = new QuoteBar(utcTime, symbol, bidBar, 0, askBar, 0);
            SymbolUpdate(quoteBar);
        }
    }
}
