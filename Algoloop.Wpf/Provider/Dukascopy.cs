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
using QuantConnect.ToolBox.DukascopyDownloader;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Algoloop.Provider
{
    public class Dukascopy : ProviderBase
    {
        private const string _market = "dukascopy";
        private readonly DateTime _firstDate = new DateTime(2003, 05, 05);

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

        public override void Download(MarketModel market, SettingModel settings)
        {
            if (market == null) throw new ArgumentNullException(nameof(market));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            IList<string> symbols = market.Symbols.Where(x => x.Active).Select(m => m.Id).ToList();
            if (!symbols.Any())
            {
                market.Active = false;
                UpdateSymbols(market);
                return;
            }

            string resolution = market.Resolution.Equals(Resolution.Tick) ? "all" : market.Resolution.ToString();
            DateTime fromDate = market.LastDate.Date.AddDays(1);
            if (fromDate >= DateTime.Today)
            {
                // Do not download today data
                market.Active = false;
                return;
            }

            if (fromDate < _firstDate)
            {
                fromDate = _firstDate;
            }

            DateTime toDate = fromDate.AddDays(1).AddTicks(-1);
            string from = fromDate.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string to = toDate.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
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
                ["data-directory"] = settings.DataFolder,
                ["data-folder"] = settings.DataFolder
            };

            // Download active symbols
            bool ok = base.RunProcess("QuantConnect.ToolBox.exe", args, config);
            if (!ok)
            {
                market.Active = false;
                return;
            }

            market.LastDate = fromDate;
            UpdateSymbols(market);
        }

        private void UpdateSymbols(MarketModel market)
        {
            var all = new List<SymbolModel>();
            all.AddRange(_majors.Select(m => new SymbolModel(m, _market, SecurityType.Forex) { Active = false, Properties = new Dictionary<string, object> { { "Category", "Majors" } } }));
            all.AddRange(_crosses.Select(m => new SymbolModel(m, _market, SecurityType.Forex) { Active = false, Properties = new Dictionary<string, object> { { "Category", "Crosses" } } }));
            all.AddRange(_metals.Select(m => new SymbolModel(m, _market, SecurityType.Cfd) { Active = false, Properties = new Dictionary<string, object> { { "Category", "Metals" } } }));
            all.AddRange(_indices.Select(m => new SymbolModel(m, _market, SecurityType.Cfd) { Active = false, Properties = new Dictionary<string, object> { { "Category", "Indices" } } }));

            // Exclude unknown symbols
            var downloader = new DukascopyDataDownloader();
            IEnumerable<SymbolModel> actual = all.Where(m => downloader.HasSymbol(m.Id));
            UpdateSymbols(market, actual, false);
        }
    }
}
