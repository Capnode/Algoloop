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
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.ToolBox.DukascopyDownloader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Algoloop.Provider
{
    public class Dukascopy : IProvider
    {
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

        public void Download(MarketModel model, SettingService settings, IList<string> symbols)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            Config.Set("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");
            Config.Set("data-directory", settings.DataFolder);

            string resolution = model.Resolution.Equals(Resolution.Tick) ? "all" : model.Resolution.ToString();
            DateTime fromDate = model.LastDate.Date.AddDays(1);
            if (fromDate >= DateTime.Today)
            {
                // Do not download today data
                model.Active = false;
                return;
            }

            if (fromDate < _firstDate )
            {
                fromDate = _firstDate;
            }

            DukascopyDownloaderProgram.DukascopyDownloader(symbols, resolution, fromDate, fromDate.AddDays(1).AddTicks(-1));
            model.LastDate = fromDate;
        }

        public IEnumerable<SymbolModel> GetAllSymbols(MarketModel market, SettingService settings)
        {
            var list = new List<SymbolModel>();
            list.AddRange(_majors.Select(m => new SymbolModel(m) { Properties = new Dictionary<string, object> { { "Category", "Majors" } } }));
            list.AddRange(_crosses.Select(m => new SymbolModel(m) { Properties = new Dictionary<string, object> { { "Category", "Crosses" } } }));
            list.AddRange(_metals.Select(m => new SymbolModel(m) { Properties = new Dictionary<string, object> { { "Category", "Metals" } } }));
            list.AddRange(_indices.Select(m => new SymbolModel(m) { Properties = new Dictionary<string, object> { { "Category", "Indices" } } }));

            var downloader = new DukascopyDataDownloader();
            return list.Where(m => downloader.HasSymbol(m.Name));
        }
    }
}
