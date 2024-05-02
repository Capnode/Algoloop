/*
 * Copyright 2022 Capnode AB
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
using Algoloop.ToolBox.MetastockConverter;
using QuantConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;

namespace Algoloop.Wpf.ViewModels.Internal.Provider
{
    internal class Metastock : ProviderBase
    {
        private const string MarketPlace = "Marketplace";

        public override void GetUpdate(ProviderModel market, Action<object> update, CancellationToken cancel)
        {
            if (market == null) throw new ArgumentNullException(nameof(market));
            if (!market.Resolution.Equals(Resolution.Daily)) throw new ArgumentException(nameof(market.Resolution));

            // Update symbol list
            IEnumerable<SymbolModel> actual = MetastockConverterProgram.GetInstruments(market.SourceFolder);
            UpdateSymbols(market, actual, update);

            // Setup convert parameters
            DateTime utsNow = DateTime.UtcNow;
            string[] args =
            {
                "--app=MetastockConverter",
                $"--source-dir={market.SourceFolder}",
                $"--destination-dir={Globals.DataFolder}",
            };
            IDictionary<string, string> config = new Dictionary<string, string>
            {
                ["data-directory"] = Globals.DataFolder,
                ["cache-location"] = Globals.DataFolder,
                ["data-folder"] = Globals.DataFolder
            };

            // Download active symbols
            RunProcess("Algoloop.ToolBox.exe", args, config, cancel);
            market.LastDate = utsNow.ToLocalTime();
            market.Active = false;
        }

        protected static new void UpdateSymbols(
            ProviderModel market,
            IEnumerable<SymbolModel> actual,
            Action<object> update,
            bool addSymbols = true)
        {
            Contract.Requires(market != null, nameof(market));
            Contract.Requires(actual != null, nameof(actual));

            RemoveAllAutoLists(market);

            bool symbolsChanged = false;
            bool listsChanged = false;
            List<SymbolModel> obsoleteSymbols = market.Symbols.ToList();
            foreach (SymbolModel item in actual)
            {
                // Add or update symbol
                SymbolModel symbol = market.Symbols.FirstOrDefault(x => x.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase)
                    && (x.Market.Equals(item.Market, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(x.Market))
                    && (x.Security.Equals(item.Security) || x.Security.Equals(SecurityType.Base)));
                if (symbol == default)
                {
                    symbol = item;
                    symbolsChanged = true;
                    if (addSymbols)
                    {
                        market.Symbols.Add(symbol);
                    }
                }
                else if (!symbol.Equals(item))
                {
                    // Update properties
                    symbol.Name = item.Name;
                    symbol.Market = item.Market;
                    symbol.Security = item.Security;
                    symbol.Properties = item.Properties;
                    symbolsChanged = true;
                    obsoleteSymbols.Remove(symbol);
                }
                else
                {
                    obsoleteSymbols.Remove(symbol);
                    symbol = item;
                }

                // Skip adding to list if not active
                if (!symbol.Active) continue;

                // Add symbol to lists
                if (!symbol.Properties.TryGetValue(MarketPlace, out object value)) continue;
                string country = value as string;
                if (string.IsNullOrEmpty(country)) continue;
                listsChanged |= AddSymbolToAutoList(market, symbol, country);
            }

            // Remove obsolete symbols
            foreach (SymbolModel old in obsoleteSymbols)
            {
                market.Symbols.Remove(old);
                foreach (ListModel list in market.Lists)
                {
                    if (list.Symbols.Remove(old))
                    {
                        listsChanged = true;
                    }
                }
            }

            // Update symbols
            if (symbolsChanged)
            {
                update?.Invoke(market.Symbols);
            }

            // Update lists
            if (listsChanged)
            {
                update?.Invoke(market.Lists);
            }
        }
    }
}
