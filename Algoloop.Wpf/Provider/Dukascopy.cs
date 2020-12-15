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
using Algoloop.Service;
using Algoloop.Wpf.Common;
using QuantConnect;
using QuantConnect.Logging;
using QuantConnect.ToolBox.DukascopyDownloader;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Algoloop.Provider
{
    public class Dukascopy : IProvider, IDisposable
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
        private ConfigProcess _process;
        private bool _isDisposed;

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Dukascopy()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Register(SettingModel settings)
        {
        }

        public void Download(MarketModel market, SettingModel settings)
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

            // Download active symbols
            bool error = false;
            _process = new ConfigProcess(
                "QuantConnect.ToolBox.exe",
                string.Join(" ", args),
                Directory.GetCurrentDirectory(),
                true,
                (line) => Log.Trace(line),
                (line) =>
                {
                    error = true;
                    Log.Error(line);
                });

            StringDictionary a = _process.Environment;
            // Set config file
            IDictionary<string, string> config = _process.Config;
            string exeFolder = MainService.GetProgramFolder();
            config["debug-mode"] = "true";
            config["composer-dll-directory"] = exeFolder;
            config["data-directory"] = settings.DataFolder;
            config["data-folder"] = settings.DataFolder;
            config["results-destination-folder"] = ".";
            config["plugin-directory"] = ".";
            config["log-handler"] = "CompositeLogHandler";
            config["map-file-provider"] = "LocalDiskMapFileProvider";
            config["#command"] = "QuantConnect.ToolBox.exe";
            config["#parameters"] = string.Join(" ", args);
            config["#work-directory"] = Directory.GetCurrentDirectory();

            // Start process
            _process.Start();
            if (!_process.WaitForExit())
            {
                error = true;
            }
            _process.Dispose();
            _process = null;

            if (error)
            {
                market.Active = false;
                return;
            }

            market.LastDate = fromDate;
            UpdateSymbols(market);
        }

        public void Abort()
        {
            _process?.Abort();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _process?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _isDisposed = true;
            }
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
            foreach (SymbolModel symbol in all.Where(m => downloader.HasSymbol(m.Id)))
            {
                SymbolModel item = market.Symbols.FirstOrDefault(
                    m => m.Id.Equals(symbol.Id, StringComparison.OrdinalIgnoreCase));
                if (item == null)
                {
                    // Add symbol
                    market.Symbols.Add(symbol);
                }
                else
                {
                    // Update properties
                    item.Properties = symbol.Properties;
                }
            }
        }
    }
}
