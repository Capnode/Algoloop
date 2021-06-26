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
using SuperSocket.ClientEngine;
using System;
using System.Threading;
using WebSocket4Net;

namespace Algoloop.Brokerages.FxcmRest
{
    public class FxcmSocket : IDisposable
    {
        private enum MessageType { Open, Close, Ping, Pong, Message, Upgrade, Noop };
        private enum ActionType { Connect, Disconnect, Event, Ack, Error, BinaryEvent, BinaryAck };

        private readonly WebSocket _webSocket;
        private readonly ManualResetEvent _hold = new(false);

        internal Action<object> AccountsUpdate { get; set; }
        internal Action<object> SymbolUpdate { get; set; }

        public string Sid { get; private set; }
        public bool IsAlive => _webSocket.Handshaked;

        public FxcmSocket(Uri uri, string key)
        {
            string url = $"wss://{uri.Host}:{uri.Port}/socket.io/?EIO=3&transport=websocket&access_token={key}";
            _webSocket = new WebSocket(url);
            _webSocket.Opened += OnOpened;
            _webSocket.Error += OnError;
            _webSocket.Closed += OnClosed;
            _webSocket.MessageReceived += OnMessageReceived;
        }

        public void Dispose()
        {
            _hold.Dispose();
            _webSocket.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Connect()
        {
            Log.Trace("Connect");
            _webSocket.Open();
            if (!_hold.WaitOne(TimeSpan.FromSeconds(20))) throw new ApplicationException($"{GetType().Name} Failed to login");
            if (!_webSocket.Handshaked) throw new ApplicationException($"{GetType().Name} Failed to login");
        }

        public void Close()
        {
            Log.Trace("Close");
            _webSocket.Close();
        }


        private void OnOpened(object sender, EventArgs e)
        {
            Log.Trace("OnOpened");
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Log.Trace("OnError");
            Log.Error(e.Exception);
            _hold.Set();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            Log.Trace("OnClosed");
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message.StartsWith("0", StringComparison.OrdinalIgnoreCase))
            {
                // 0{"sid":"oTlhP94ieIujcA7aAVdn","upgrades":[],"pingInterval":25000,"pingTimeout":5000}
                OpenConnect(e.Message[1..]);
            }
            else if (e.Message.StartsWith("40", StringComparison.OrdinalIgnoreCase))
            {
                // 40
                MessageConnect(e.Message[2..]);
            }
            else if (e.Message.StartsWith("42", StringComparison.OrdinalIgnoreCase))
            {
                // 42["EUR/USD","{\"Updated\":1614843030623,\"Rates\":[1.20526,1.20539,1.2068999999999999,1.20429],\"Symbol\":\"EUR/USD\"}"]
                MessageEvent(e.Message[2..]);
            }
        }

        private void OpenConnect(string json)
        {
            Log.Trace($"OpenConnect {json}");
            JObject jo = JObject.Parse(json);
            Sid = jo["sid"].ToString();
        }

        private void MessageConnect(string json)
        {
            Log.Trace($"MessageConnect {json}");
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
