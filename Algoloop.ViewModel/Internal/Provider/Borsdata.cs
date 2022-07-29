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
using Algoloop.ToolBox.Borsdata;
using QuantConnect;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Algoloop.ViewModel.Internal.Provider
{
    internal class Borsdata : ProviderBase
    {
        public override void GetUpdate(ProviderModel market, Action<object> update)
        {
            if (market == null) throw new ArgumentNullException(nameof(market));
            if (!market.Resolution.Equals(Resolution.Daily)) throw new ArgumentException(nameof(market.Resolution));

            // Update symbol list
            using var downloader = new BorsdataDataDownloader(market.ApiKey);
            IEnumerable<SymbolModel> actual = downloader.GetInstruments();
            bool addNew = market.Symbols.Count != 1; // For test
            UpdateSymbols(market, actual, addNew);

            // Setup download parameters
            IList<string> symbols = market.Symbols.Select(m => m.Id).ToList();
            DateTime utsNow = DateTime.UtcNow;
            string from = market.LastDate.ToUniversalTime().ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string to = utsNow.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string[] args =
            {
                "--app=BorsdataDownloader",
                $"--from-date={from}",
                $"--to-date={to}",
                $"--api-key={market.ApiKey}",
                $"--tickers={string.Join(",", symbols)}"
            };
            IDictionary<string, string> config = new Dictionary<string, string>
            {
                ["data-directory"] = Globals.DataFolder,
                ["cache-location"] = Globals.DataFolder,
                ["data-folder"] = Globals.DataFolder
            };

            // Download active symbols
            RunProcess("Algoloop.ToolBox.exe", args, config);
            market.LastDate = utsNow.ToLocalTime();
            market.Active = false;
        }
    }
}
