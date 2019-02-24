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
using QuantConnect.Configuration;
using QuantConnect.ToolBox.DukascopyDownloader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Algoloop.Provider
{
    class Dukascopy : IProvider
    {
        private readonly IEnumerable<string> _symbols = new []
        {
            "AUDCAD", "AUDCHF", "AUDJPY", "AUDNZD", "AUDSGD", "AUDUSD", "AU200AUD", "BRIDXBRL", "BCOUSD", "CADCHF", "CADHKD", "CADJPY", "CH20CHF", "CHFJPY", "CHFPLN", "CHFSGD",
            "XCUUSD", "DE30EUR", "ES35EUR", "EURAUD", "EURCAD", "EURCHF", "EURDKK", "EURGBP", "EURHKD", "EURHUF", "EURJPY", "EURMXN", "EURNOK", "EURNZD", "EURPLN", "EURRUB",
            "EURSEK", "EURSGD", "EURTRY", "EURUSD", "EURZAR", "EU50EUR", "FR40EUR", "GBPAUD", "GBPCAD", "GBPCHF", "GBPJPY", "GBPNZD", "GBPUSD", "UK100GBP", "HKDJPY", "HK33HKD",
            "IT40EUR", "JP225JPY", "WTICOUSD", "MXNJPY", "NATGASUSD", "NL25EUR", "NZDCAD", "NZDCHF", "NZDJPY", "NZDSGD", "NZDUSD", "XPDUSD", "XPTUSD", "SGDJPY", "US30USD",
            "SPX500USD", "NAS100USD", "USDBRL", "USDCAD", "USDCHF", "USDCNY", "USDDKK", "USDHKD", "USDHUF", "USDJPY", "USDMXN", "USDNOK", "USDPLN", "USDRUB", "USDSEK", "USDSGD",
            "USDTRY", "USDZAR", "XAGUSD", "XAUUSD", "ZARJPY"
        };

        public void Download(MarketModel model, SettingsModel settings, IList<string> symbols)
        {
            Config.Set("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");
            Config.Set("data-directory", settings.DataFolder);

            string resolution = model.Resolution.Equals(Resolution.Tick) ? "all" : model.Resolution.ToString();
            DateTime fromDate = model.FromDate.Date;
            if (fromDate < DateTime.Today)
            {
                DateTime nextDate = fromDate.AddDays(1);
                DukascopyDownloaderProgram.DukascopyDownloader(symbols, resolution, fromDate, nextDate.AddMilliseconds(-1));
                model.FromDate = nextDate;
            }
            model.Active = model.FromDate < DateTime.Today;
        }

        public IEnumerable<SymbolModel> GetAllSymbols(MarketModel market)
        {
            return _symbols.Select(m => new SymbolModel() { Name = m });
        }
    }
}
