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
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using System;
using System.Threading;

namespace Algoloop.Brokerages.FxcmRest
{
    public class FxcmSocket : IDisposable
    {
        private static string _msgOpen = "0";
        private static string _msgClose = "1";
        private static string _msgPing = "2";
        private static string _msgPong = "3";
        private static string _msgMessage = "4";
        private static string _msgUpgrade = "5";
        private static string _msgNoop = "6";
        private static string _msgMessageConnect = "40";
        private static string _msgMessageEvent = "42";

        private enum ActionType { Connect, Disconnect, Event, Ack, Error, BinaryEvent, BinaryAck };

        private readonly IWebSocket _webSocket;
        private readonly ManualResetEvent _hold = new(false);
        private Timer _keepAliveTimer;

        internal Action<object> AccountsUpdate { get; set; }
        internal Action<object> SymbolUpdate { get; set; }

        public string Sid { get; private set; }

        public bool IsAlive => _webSocket.IsOpen;

        public FxcmSocket(Uri uri, string key)
        {
            _webSocket = new WebSocketClientWrapper();
            _webSocket.Initialize($"wss://{uri.Host}:{uri.Port}/socket.io/?transport=websocket&access_token={key}");
            _webSocket.Open += OnOpen;
            _webSocket.Closed += OnClosed;
            _webSocket.Error += OnError;
            _webSocket.Message += OnMessage;
            _keepAliveTimer = new((x) => _webSocket.Send(_msgPing));
        }

        public void Dispose()
        {
            _hold.Dispose();
            _keepAliveTimer?.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Connect()
        {
            Log.Trace("Connect");
            _webSocket.Connect();
            if (!_hold.WaitOne(TimeSpan.FromSeconds(20))) throw new ApplicationException($"{GetType().Name} Failed to login");
            if (!_webSocket.IsOpen) throw new ApplicationException($"{GetType().Name} Failed to login");
        }

        public void Close()
        {
            Log.Trace("Close");
            _webSocket.Close();
            _keepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void OnOpen(object sender, EventArgs e)
        {
            Log.Trace("OnOpen");
        }

        private void OnClosed(object sender, WebSocketCloseData e)
        {
            Log.Trace($"OnClosed {e.Reason}");
        }

        private void OnError(object sender, WebSocketError e)
        {
            Log.Trace($"OnError {e.Message}");
            _hold.Set();
        }

        private void OnMessage(object sender, WebSocketMessage e)
        {
            Log.Trace($"OnMessage {e.Message}");

            if (e.Message.StartsWith(_msgPong, StringComparison.OrdinalIgnoreCase))
            {
                ; // Do nothing
            }
            else if (e.Message.StartsWith(_msgOpen, StringComparison.OrdinalIgnoreCase))
            {
                // 0{"sid":"oTlhP94ieIujcA7aAVdn","upgrades":[],"pingInterval":25000,"pingTimeout":5000}
                OpenConnect(e.Message[1..]);
            }
            else if (e.Message.StartsWith(_msgMessageConnect, StringComparison.OrdinalIgnoreCase))
            {
                // 40
                MessageConnect(e.Message[2..]);
            }
            else if (e.Message.StartsWith(_msgMessageEvent, StringComparison.OrdinalIgnoreCase))
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
            int interval = jo["pingInterval"].ToInt();
            _keepAliveTimer.Change(interval, interval);
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
