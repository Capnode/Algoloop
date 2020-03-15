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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Algoloop.Model;
using Algoloop.Service;
using QuantConnect;

namespace Algoloop.Provider
{
    public class Usa : IProvider
    {
        public Usa()
        {

        }

        public void Download(MarketModel market, SettingService settings, IList<string> symbols)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<SymbolModel> GetAllSymbols(MarketModel market, SettingService settings)
        {
            if (market == null) throw new ArgumentNullException(nameof(market));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            string directory = Path.Combine(
                settings.DataFolder,
                SecurityType.Equity.ToString().ToLowerInvariant(),
                market.Provider);
            string dailyFolder = Path.Combine(directory, "daily");

            DirectoryInfo d = new DirectoryInfo(dailyFolder);
            if (!d.Exists)
            {
                return Enumerable.Empty<SymbolModel>();
            }

            IEnumerable<SymbolModel> symbols = d.GetFiles("*.zip")
                .Select(m => Path.GetFileNameWithoutExtension(m.Name))
                .Select(n => new SymbolModel(n));
            return symbols;
        }
    }
}
