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
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using Algoloop.Model;
using QuantConnect;
using QuantConnect.Logging;

namespace Algoloop.ViewModel.Internal.Provider
{
    internal class QuantConnect : ProviderBase
    {
        private const string Version = "15022";
        private const string Security = "Security";
        private const string Zip = ".zip";

        public override void GetUpdate(ProviderModel provider, Action<object> update)
        {
            Contract.Requires(provider != null);
            var uri = new Uri($"https://github.com/QuantConnect/Lean/archive/refs/tags/{Version}.zip");
            string extract = $"Lean-{Version}/Data/";
            string filename = "github.zip";
            Log.Trace($"Download {uri}");
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(uri).Result;
                using (var fs = new FileStream(filename, FileMode.CreateNew))
                {
                    response.Content.CopyToAsync(fs).Wait();
                }
            }

            Log.Trace($"Unpack {uri}");
            IList<SymbolModel> symbols = new List<SymbolModel>();
            string dest = Globals.DataFolder;
            using (var archive = new ZipArchive(File.OpenRead(filename)))
            {
                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    // skip directories
                    if (string.IsNullOrEmpty(file.Name)) continue;
                    if (!file.FullName.StartsWith(extract, StringComparison.OrdinalIgnoreCase)) continue;
                    string path = file.FullName[extract.Length..];
                    AddSymbol(symbols, path);
                    string destPath = Path.Combine(dest, path);
                    var outputFile = new FileInfo(destPath);
                    if (!outputFile.Directory.Exists)
                    {
                        outputFile.Directory.Create();
                    }

                    file.ExtractToFile(outputFile.FullName, true);
                }
            }

            // Adjust lastdate
            if (provider.Symbols.Any())
            {
                provider.LastDate = DateTime.Today;
            }

            File.Delete(filename);

            // Update symbol list
            UpdateSymbols(provider, symbols, true);

            Log.Trace($"Unpack {uri} completed");
            provider.Active = false;
        }

        private static void AddSymbol(IList<SymbolModel> symbols, string path)
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

            if (ticker.Contains('.'))
            {
                if (!ticker.EndsWith(Zip, StringComparison.OrdinalIgnoreCase)) return;
                ticker = ticker[..^Zip.Length];
            }

            if (symbols
                .Where(x => x.Id.Equals(ticker, StringComparison.OrdinalIgnoreCase)
                    && x.Market.Equals(marketName, StringComparison.OrdinalIgnoreCase)
                    && x.Security.Equals(security))
                .Any())
            {
                return;
            }

            var symbol = new SymbolModel(ticker, marketName, securityType)
            {
                Properties = new Dictionary<string, object> { { Security, securityType } }
            };

            symbols.Add(symbol);
        }
    }
}
