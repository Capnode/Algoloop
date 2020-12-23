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
using QuantConnect.Brokerages.Fxcm;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Algoloop.Provider
{
    public class Fxcm : ProviderBase
    {
        private readonly DateTime _firstDate = new DateTime(2003, 05, 05);

        public override void Download(MarketModel market, SettingModel settings)
        {
            if (market == null) throw new ArgumentNullException(nameof(market));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            IList<string> symbols = market.Symbols.Where(x => x.Active).Select(m => m.Id).ToList();
            string resolution = market.Resolution.Equals(Resolution.Tick) ? "all" : market.Resolution.ToString();
            DateTime fromDate = market.LastDate < _firstDate ? _firstDate : market.LastDate.Date;
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
                ["data-directory"] = settings.DataFolder,
                ["data-folder"] = settings.DataFolder,
                ["fxcm-user-name"] = market.Login,
                ["fxcm-password"] = market.Password,
                ["fxcm-terminal"] = market.Access.ToStringInvariant()
            };

            DateTime now = DateTime.Now;
            if (RunProcess("QuantConnect.ToolBox.exe", args, config))
            {
                if (toDate > now)
                {
                    market.Active = false;
                }
                else
                {
                    market.LastDate = toDate;
                }
            }
            else
            {
                market.Active = false;
            }

            UpdateSymbols(market);
        }

        private void UpdateSymbols(MarketModel market)
        {
            List<Symbol> symbols = FxcmSymbolMapper.KnownSymbols;
            IEnumerable<SymbolModel> actual = symbols.Select(
                m => new SymbolModel(m.ID.Symbol, m.ID.Market, m.ID.SecurityType) { Active = false } );
            UpdateSymbols(market, actual, false);
        }
    }
}
