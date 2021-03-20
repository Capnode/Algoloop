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
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Fxcm;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;

namespace Algoloop.Wpf.Provider
{
    public class Fxcm : ProviderBase
    {
        private readonly DateTime _firstDate = new(2003, 05, 05);
        private const string _fxcmServer = "http://www.fxcorporate.com/Hosts.jsp";
        private SettingModel _settings;
        private FxcmBrokerage _brokerage;

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                if (_brokerage != null)
                {
                    _brokerage.Dispose();
                }
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null

            base.Dispose(disposing);
        }

        public override bool Register(SettingModel settings)
        {
            Contract.Requires(settings != null);
            _settings = settings;
            return base.Register(settings);
        }

        public override void Login(ProviderModel provider)
        {
            Contract.Requires(provider != null);

            _brokerage = new FxcmBrokerage(
                null,
                null,
                Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager")),
                _fxcmServer,
                provider.Access.ToString(),
                provider.Login,
                provider.Password,
                provider.Login);

            _brokerage.Message += OnMessage;
            _brokerage.AccountChanged += OnAccountChanged;
            _brokerage.OptionPositionAssigned += OnOptionPositionAssigned;
            _brokerage.OrderStatusChanged += OnOrderStatusChanged;
            _brokerage.Connect();
        }

        public override void Logout()
        {
            _brokerage.Disconnect();
            _brokerage.Dispose();
            _brokerage = null;
        }

        public override void GetMarketData(ProviderModel provider, Action<object> update)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            IList<string> symbols = provider.Symbols.Where(x => x.Active).Select(m => m.Id).ToList();
            string resolution = provider.Resolution.Equals(Resolution.Tick) ? "all" : provider.Resolution.ToString();
            DateTime fromDate = provider.LastDate < _firstDate ? _firstDate : provider.LastDate.Date;
            DateTime toDate = fromDate.AddDays(1);
            string from = fromDate.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string to = fromDate.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string[] args =
            {
                "--app=FxcmDownloader",
                $"--from-date={from}",
                $"--to-date={to}",
                $"--resolution={resolution}",
                $"--tickers={string.Join(",", symbols)}"
            };

            IDictionary<string, string> config = new Dictionary<string, string>
            {
                ["data-directory"] = _settings.DataFolder,
                ["data-folder"] = _settings.DataFolder,
                ["fxcm-user-name"] = provider.Login,
                ["fxcm-password"] = provider.Password,
                ["fxcm-terminal"] = provider.Access.ToStringInvariant()
            };

            DateTime now = DateTime.Now;
            RunProcess("QuantConnect.ToolBox.exe", args, config);
            if (toDate > now)
            {
                provider.Active = false;
            }
            else
            {
                provider.LastDate = toDate;
            }

            UpdateSymbols(provider);
        }

        private static void UpdateSymbols(ProviderModel market)
        {
            List<Symbol> symbols = FxcmSymbolMapper.KnownSymbols;
            IEnumerable<SymbolModel> actual = symbols.Select(
                m => new SymbolModel(m.ID.Symbol, m.ID.Market, m.ID.SecurityType) { Active = false } );
            UpdateSymbols(market, actual, false);
        }

        private void OnMessage(object sender, BrokerageMessageEvent message)
        {
            var brokerage = sender as Brokerage;
            Log.Trace($"{brokerage.Name}: {message.GetType()}: {message}");
        }

        private void OnAccountChanged(object sender, AccountEvent e)
        {
            var brokerage = sender as Brokerage;
            Log.Trace($"{brokerage.Name}: {e.GetType()}: {e}");
        }

        private void OnOrderStatusChanged(object sender, OrderEvent e)
        {
            var brokerage = sender as Brokerage;
            Log.Trace($"{brokerage.Name}: {e.GetType()}: {e}");
        }

        private void OnOptionPositionAssigned(object sender, OrderEvent e)
        {
            var brokerage = sender as Brokerage;
            Log.Trace($"{brokerage.Name}: {e.GetType()}: {e}");
        }
    }
}
