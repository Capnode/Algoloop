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

using System;
using System.IO;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using QuantConnect.ToolBox.CoarseUniverseGenerator;
using QuantConnect.Logging;
using Algoloop.Brokerages;
using Algoloop.Model;

namespace Algoloop.ToolBox.MetastockConverter
{
    public static class MetastockConverterProgram
    {
        static private readonly TimeSpan Daily = TimeSpan.FromDays(1);

        public static void MetastockConverter(string sourceDirectory, string destinationDirectory)
        {
            //Document the process:
            Console.WriteLine("Algoloop.ToolBox: Metastock Converter: ");
            Console.WriteLine("==============================================");
            Console.WriteLine("The Metastock converter transforms Metastock data into the LEAN Algorithmic Trading Engine Data Format.");
            Console.WriteLine("Parameters required: --source-dir= --destination-dir= ");
            Console.WriteLine("   1> Source Directory of Unzipped Metastock Data.");
            Console.WriteLine("   2> Destination Directory of LEAN Data Folder. (Typically located under Lean/Data)");
            Console.WriteLine(" ");
            Console.WriteLine("NOTE: THIS WILL OVERWRITE ANY EXISTING FILES.");
            if (sourceDirectory.IsNullOrEmpty() || destinationDirectory.IsNullOrEmpty())
            {
                Console.WriteLine("1. Source Metastock source directory: ");
                sourceDirectory = (Console.ReadLine() ?? "");
                Console.WriteLine("2. Destination LEAN Data directory: ");
                destinationDirectory = (Console.ReadLine() ?? "");
            }

            try
            {
                // Remove folder
                string folder = Path.Combine(
                    destinationDirectory,
                    SecurityType.Equity.SecurityTypeToLower(),
                    Market.Metastock);
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }

                // Create folder structure
                Directory.CreateDirectory(MapFile.GetMapFilePath(Market.Metastock, SecurityType.Equity));

                // Start converting
                ReadFolder(new DirectoryInfo(sourceDirectory), destinationDirectory);
                GenerateCoarseFiles();
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
}

        public static IEnumerable<SymbolModel> GetInstruments(string folder)
        {
            var instruments = new List<SymbolModel>();
            var dir = new DirectoryInfo(folder);
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                IEnumerable<SymbolModel> list = GetInstruments(subDir.FullName);
                instruments.AddRange(list);
            }


            // Read securities
            var msDir = new MsDirectory(folder);
            foreach (MsSecurity security in msDir.GetSecurities())
            {
                string ticker = MarketHelper.ValidateSymbolName(security.Symbol);
                var symbol = new SymbolModel(ticker, Market.Metastock, SecurityType.Equity)
                {
                    Name = security.Name,
                    Properties = new Dictionary<string, object>
                    {
                        { "Ticker", ticker },
                        { "Marketplace", dir.Name },
                    }
                };
                instruments.Add(symbol);
            }

            return instruments;
        }

        private static void ReadFolder(DirectoryInfo dir, string destinationDirectory)
        {
            // Read subfolders
            foreach (DirectoryInfo subDir in dir.GetDirectories())
                ReadFolder(subDir, destinationDirectory);

            // Read securities
            var msDir = new MsDirectory(dir.FullName);
            foreach (MsSecurity security in msDir.GetSecurities())
            {
                string ticker = MarketHelper.ValidateSymbolName(security.Symbol);
                Symbol symbol = Symbol.Create(ticker, SecurityType.Equity, Market.Metastock);
                var bars = new List<TradeBar>();
                List<MsPrice> prices = security.GetPriceList();
                foreach (MsPrice price in prices)
                {
                    try
                    {
                        var bar = new TradeBar
                        {
                            Symbol = symbol,
                            Period = Daily,
                            Time = price.Date,
                            Open = ToDecimal(price.Open),
                            High = ToDecimal(price.High),
                            Low = ToDecimal(price.Low),
                            Close = ToDecimal(price.Close),
                            Volume = (decimal)price.Volume
                        };
                        bars.Add(bar);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        // Invalid date
                        Log.Trace($"Error found in {security.Symbol}: {ex.Message}", true);
                    }
                }

                bars.ValidateTadeBars();
                BaseData first = bars.FirstOrDefault();
                if (first == default) continue;
                var datawriter = new LeanDataWriter(Resolution.Daily, symbol, destinationDirectory);
                datawriter.Write(bars);
                UpdateMapFile(symbol.Value, first.Time.Date);
            }
        }

        private static decimal ToDecimal(float price)
        {
            if (price < 10)
                return (decimal)Math.Round(price, 5);
            else
                return (decimal)Math.Round(price, 3);
        }

        private static void UpdateMapFile(string symbol, DateTime date)
        {
            MapFile mapFile;
            IEnumerable<MapFileRow> presentRows;
            string mapRoot = MapFile.GetMapFilePath(Market.Metastock, SecurityType.Equity);
            string path = Path.Combine(mapRoot, symbol.ToLowerInvariant() + ".csv");

            // Check if date is already mapped
            if (File.Exists(path))
            {
                IDataProvider dataProvider = Composer.Instance.GetExportedValueByTypeName<IDataProvider>(
                    Config.Get("data-provider", "DefaultDataProvider"));
                presentRows = MapFileRow.Read(path, Market.Metastock, SecurityType.Equity, dataProvider);
                mapFile = new MapFile(symbol, presentRows);
                if (mapFile.HasData(date)) return;
            }

            // Create new mapfile with date
            IList<MapFileRow> newRows = new List<MapFileRow>
            {
                new MapFileRow(date, symbol),
                new MapFileRow(new DateTime(2050, 12, 31), symbol)
            };
            mapFile = new MapFile(symbol, newRows);
            mapFile.WriteToCsv(Market.Metastock, SecurityType.Equity);
        }

        private static bool GenerateCoarseFiles()
        {
            var dailyDataFolder = new DirectoryInfo(Path.Combine(
                Globals.DataFolder,
                SecurityType.Equity.SecurityTypeToLower(),
                Market.Metastock,
                Resolution.Daily.ResolutionToLower()));
            var destinationFolder = new DirectoryInfo(Path.Combine(
                Globals.DataFolder,
                SecurityType.Equity.SecurityTypeToLower(),
                Market.Metastock,
                "fundamental",
                "coarse"));
            var finePath = Path.Combine(
                Globals.DataFolder,
                SecurityType.Equity.SecurityTypeToLower(),
                Market.Metastock,
                "fundamental",
                "fine");
            Directory.CreateDirectory(finePath);
            var fineFolder = new DirectoryInfo(finePath);
            var blackListedTickersFile = new FileInfo("blacklisted-tickers.txt");
            var reservedWordPrefix = Config.Get("reserved-words-prefix", "quantconnect-");
            var dataProvider = Composer.Instance.GetExportedValueByTypeName<IDataProvider>(
                Config.Get("data-provider", "DefaultDataProvider"));
            var mapFileProvider = new LocalDiskMapFileProvider();
            var factorFileProvider = new LocalDiskFactorFileProvider();
            factorFileProvider.Initialize(mapFileProvider, dataProvider);
            var generator = new CoarseUniverseGeneratorProgram(
                dailyDataFolder,
                destinationFolder,
                fineFolder,
                Market.Metastock,
                blackListedTickersFile,
                reservedWordPrefix,
                mapFileProvider,
                factorFileProvider);
            return generator.Run();
        }
    }
}
