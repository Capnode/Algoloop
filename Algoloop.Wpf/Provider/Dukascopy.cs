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
using QuantConnect.Securities;
using QuantConnect.ToolBox.DukascopyDownloader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;

namespace Algoloop.Wpf.Provider
{
    public class Dukascopy : ProviderBase
    {
        private const string _market = "dukascopy";
        private readonly DateTime _firstDate = new(2003, 05, 05);

        private readonly IEnumerable<string> _majors = new[] { "AUDUSD", "EURUSD", "GBPUSD", "NZDUSD", "USDCAD", "USDCHF", "USDJPY" };
        private readonly IEnumerable<string> _crosses = new[]
        {
            "AUDCAD", "AUDCHF", "AUDJPY", "AUDNZD", "AUDSGD", "CADCHF", "CADHKD", "CADJPY", "CHFJPY", "CHFPLN", "CHFSGD",
            "EURAUD", "EURCAD", "EURCHF", "EURDKK", "EURGBP", "EURHKD", "EURHUF", "EURJPY", "EURMXN", "EURNOK", "EURNZD",
            "EURPLN", "EURRUB", "EURSEK", "EURSGD", "EURTRY", "EURZAR", "GBPAUD", "GBPCAD", "GBPCHF", "GBPJPY", "GBPNZD",
            "HKDJPY", "MXNJPY", "NZDCAD", "NZDCHF", "NZDJPY", "NZDSGD", "SGDJPY", "USDBRL", "USDCNY", "USDDKK", "USDHKD",
            "USDHUF", "USDMXN", "USDNOK", "USDPLN", "USDRUB", "USDSEK", "USDSGD", "USDTRY", "USDZAR", "ZARJPY"
        };
        private readonly IEnumerable<string> _metals = new[] { "XAGUSD", "XAUUSD", "WTICOUSD" };
        private readonly IEnumerable<string> _indices = new[]
        {
            "AU200AUD", "CH20CHF", "DE30EUR", "ES35EUR", "EU50EUR", "FR40EUR", "UK100GBP", "HK33HKD", "IT40EUR", "JP225JPY",
            "NL25EUR", "US30USD", "SPX500USD", "NAS100USD"
        };

        private SettingModel _settings;

        public override bool Register(SettingModel settings)
        {
            Contract.Requires(settings != null);
            _settings = settings;

            // Register market
            if (!base.Register(settings)) return false;

            // Register Market Hours
            MarketHoursDatabase marketHours = MarketHoursDatabase.FromDataFolder(settings.DataFolder);
            Debug.Assert(marketHours != null);

            var exchangeHours = new SecurityExchangeHours(
                TimeZones.Utc,
                Enumerable.Empty<DateTime>(),
                new Dictionary<DayOfWeek, LocalMarketHours>
                {
                    { DayOfWeek.Monday, new LocalMarketHours(DayOfWeek.Monday, new TimeSpan(0, 0, 0), new TimeSpan(1, 0, 0, 0)) },
                    { DayOfWeek.Tuesday, new LocalMarketHours(DayOfWeek.Tuesday, new TimeSpan(0, 0, 0), new TimeSpan(1, 0, 0, 0)) },
                    { DayOfWeek.Wednesday, new LocalMarketHours(DayOfWeek.Wednesday, new TimeSpan(0, 0, 0), new TimeSpan(1, 0, 0, 0)) },
                    { DayOfWeek.Thursday, new LocalMarketHours(DayOfWeek.Thursday, new TimeSpan(0, 0, 0), new TimeSpan(1, 0, 0, 0)) },
                    { DayOfWeek.Friday, new LocalMarketHours(DayOfWeek.Friday, new TimeSpan(0, 0, 0), new TimeSpan(22, 0, 0)) },
                    { DayOfWeek.Saturday, LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday) },
                    { DayOfWeek.Sunday, new LocalMarketHours(DayOfWeek.Friday, new TimeSpan(22, 0, 0), new TimeSpan(1, 0, 0, 0)) }
                },
                new Dictionary<DateTime, TimeSpan>(),
                new Dictionary<DateTime, TimeSpan>());

            marketHours.SetEntry(
                GetType().Name.ToLowerInvariant(),
                null,
                SecurityType.Forex,
                exchangeHours,
                TimeZones.Utc);

            return true;
        }

        public override void GetMarketData(ProviderModel market, Action<object> update)
        {
            if (market == null) throw new ArgumentNullException(nameof(market));

            IList<string> symbols = market.Symbols.Where(x => x.Active).Select(m => m.Id).ToList();
            if (!symbols.Any())
            {
                UpdateSymbols(market);
                market.Active = false;
                return;
            }

            string resolution = market.Resolution.Equals(Resolution.Tick) ? "all" : market.Resolution.ToString();
            DateTime lastDate = market.LastDate.ToUniversalTime();
            DateTime fromDate = lastDate < _firstDate ? _firstDate : lastDate.Date;
            DateTime toDate = fromDate.AddDays(1);
            string from = fromDate.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string to = toDate.AddTicks(-1).ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string[] args =
            {
                "--app=DukascopyDownloader",
                $"--from-date={from}",
                $"--to-date={to}",
                $"--resolution={resolution}",
                $"--tickers={string.Join(",", symbols)}"
            };

            IDictionary<string, string> config = new Dictionary<string, string>
            {
                ["data-directory"] = _settings.DataFolder,
                ["cache-location"] = _settings.DataFolder,
                ["data-folder"] = _settings.DataFolder
            };

            DateTime now = DateTime.UtcNow;
            RunProcess("QuantConnect.ToolBox.exe", args, config);
            if (toDate > now)
            {
                market.Active = false;
            }
            else
            {
                market.LastDate = toDate.ToLocalTime();
            }

            UpdateSymbols(market);
        }

        private void UpdateSymbols(ProviderModel market)
        {
            var all = new List<SymbolModel>();
            all.AddRange(_majors.Select(m => new SymbolModel(m, _market, SecurityType.Forex) { Active = false, Properties = new Dictionary<string, object> { { "Category", "Majors" } } }));
            all.AddRange(_crosses.Select(m => new SymbolModel(m, _market, SecurityType.Forex) { Active = false, Properties = new Dictionary<string, object> { { "Category", "Crosses" } } }));
            all.AddRange(_metals.Select(m => new SymbolModel(m, _market, SecurityType.Cfd) { Active = false, Properties = new Dictionary<string, object> { { "Category", "Metals" } } }));
            all.AddRange(_indices.Select(m => new SymbolModel(m, _market, SecurityType.Cfd) { Active = false, Properties = new Dictionary<string, object> { { "Category", "Indices" } } }));

            // Exclude unknown symbols
            var downloader = new DukascopyDataDownloader();
            IEnumerable<SymbolModel> actual = all.Where(m => downloader.HasSymbol(m.Id));
            UpdateSymbols(market, actual, true);
        }
    }
}
