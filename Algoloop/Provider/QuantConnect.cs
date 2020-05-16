/*
 * Copyright 2020 Capnode AB
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
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Algoloop.Model;
using QuantConnect;
using QuantConnect.Logging;

namespace Algoloop.Provider
{
    public class QuantConnect : IProvider
    {
        private const string _version = "1.0.1";
        private const string _security = "Security";
        private const string _market = "Market";
        private const string _zip = ".zip";

        public void Download(MarketModel model, SettingModel settings)
        {
            var uri = new Uri($"https://github.com/Capnode/Algoloop/archive/Algoloop-{_version}.zip");
            string extract = $"Algoloop-Algoloop-{_version}/Data/";
            string filename = "github.zip";
            Log.Trace($"Download {uri}");
            using (var client = new WebClient())
            {
                client.DownloadFile(uri, filename);
            }

            Log.Trace($"Unpack {uri}");
            IList<SymbolModel> symbols = new List<SymbolModel>();
            string dest = settings.DataFolder;
            using (ZipArchive archive = new ZipArchive(File.OpenRead(filename)))
            {
                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    // skip directories
                    if (string.IsNullOrEmpty(file.Name)) continue;
                    if (!file.FullName.StartsWith(extract)) continue;
                    string path = file.FullName.Substring(extract.Length);
                    AddSymbol(symbols, path);
                    string destPath = Path.Combine(dest, path);
                    FileInfo outputFile = new FileInfo(destPath);
                    if (!outputFile.Directory.Exists)
                    {
                        outputFile.Directory.Create();
                    }

                    file.ExtractToFile(outputFile.FullName, true);
                }
            }

            // Adjust lastdate
            if (model.Symbols.Any())
            {
                model.LastDate = DateTime.Today;
            }

            File.Delete(filename);

            // Update symbol list
            UpdateSymbols(model.Symbols, symbols);

            Log.Trace($"Unpack {uri} completed");
            model.Active = false;
        }

        private void AddSymbol(IList<SymbolModel> symbols, string path)
        {
            string[] split = path.Split('/');
            if (split.Length < 4) return;
            string security = split[0];
            string marketName = split[1];
            string resolution = split[2];
            string ticker = split[3];

            if (!Enum.TryParse(security, true, out SecurityType securityType)) return;

            switch (resolution)
            {
                case "daily":
                case "hour":
                case "minute":
                case "second":
                case "tick":
                    break;
                default:
                    return;
            }

            if (ticker.Contains("."))
            {
                if (!ticker.EndsWith(_zip)) return;
                ticker = ticker.Substring(0, ticker.Length - _zip.Length);
            }

            if (symbols
                .Where(x => x.Name.Equals(ticker, StringComparison.OrdinalIgnoreCase)
                    && x.Market.Equals(marketName, StringComparison.OrdinalIgnoreCase)
                    && x.Security.Equals(security))
                .Any())
            {
                return;
            }

            var symbol = new SymbolModel(ticker, marketName, securityType)
            {
                Properties = new Dictionary<string, object> { { _security, securityType } }
            };

            symbols.Add(symbol);
        }

        private void UpdateSymbols(Collection<SymbolModel> symbols, IList<SymbolModel> all)
        {
            // Collect list of obsolete symbols
            List<SymbolModel> discarded = symbols.ToList();

            foreach (SymbolModel item in all)
            {
                var symbol = symbols.FirstOrDefault(x => x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)
                    && x.Market.Equals(item.Market, StringComparison.OrdinalIgnoreCase)
                    && x.Security.Equals(item.Security));
                if (symbol == null)
                {
                    symbols.Add(item);
                }
                else
                {
                    discarded.Remove(symbol);
                }
            }

            foreach (SymbolModel old in discarded)
            {
                symbols.Remove(old);
            }
        }
    }
}
