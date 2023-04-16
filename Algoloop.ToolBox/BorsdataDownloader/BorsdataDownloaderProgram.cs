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

using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.ToolBox.CoarseUniverseGenerator;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Algoloop.ToolBox.BorsdataDownloader
{
    public static class BorsdataDownloaderProgram
    {
        /// <summary>
        /// Primary entry point to the program
        ///  tickers: list of symbols
        ///  startDate: first date (Exclusive)
        ///  endDate: last date (Inclusive)
        /// </summary>
        public static void BorsdataDownloader(
            IList<string> tickers,
            DateTime startDate,
            DateTime endDate,
            string apiKey)
        {
            if (tickers.IsNullOrEmpty() || apiKey.IsNullOrEmpty())
            {
                Console.WriteLine("BorsdataDownloader ERROR: '--tickers=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg EURUSD,USDJPY");
                Console.WriteLine("--apiKey=<apikey>");
                Environment.Exit(1);
            }

            try
            {
                Directory.CreateDirectory(MapFile.GetMapFilePath(Market.Borsdata, SecurityType.Equity));

                // Download the data
                using var downloader = new BorsdataDataDownloader(apiKey);
                IEnumerable<Symbol> symbols = tickers.Select(m => Symbol.Create(m, SecurityType.Equity, Market.Borsdata));
                foreach (Symbol symbol in symbols)
                {
                    var parameters = new DataDownloaderGetParameters(symbol, Resolution.Daily, startDate, endDate);
                    IEnumerable<BaseData> data = downloader.Get(parameters);
                    BaseData first = data?.FirstOrDefault();
                    if (first == default) continue;
                    LeanDataWriter writer = new (Resolution.Daily, symbol, Globals.DataFolder);
                    writer.Write(data);
                    UpdateMapFile(symbol.Value, first.Time.Date);
                }

                GenerateCoarseFiles();
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        private static void UpdateMapFile(string symbol, DateTime date)
        {
            MapFile mapFile;
            IEnumerable<MapFileRow> presentRows;
            string mapRoot = MapFile.GetMapFilePath(Market.Borsdata, SecurityType.Equity);
            string path = Path.Combine(mapRoot, symbol.ToLowerInvariant() + ".csv");

            // Check if date is already mapped
            if (File.Exists(path))
            {
                IDataProvider dataProvider = Composer.Instance.GetExportedValueByTypeName<IDataProvider>(
                    Config.Get("data-provider", "DefaultDataProvider"));
                presentRows = MapFileRow.Read(path, Market.Borsdata, SecurityType.Equity, dataProvider);
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
            mapFile.WriteToCsv(Market.Borsdata, SecurityType.Equity);
        }

        private static bool GenerateCoarseFiles()
        {
            var dailyDataFolder = new DirectoryInfo(Path.Combine(
                Globals.DataFolder,
                SecurityType.Equity.SecurityTypeToLower(),
                Market.Borsdata,
                Resolution.Daily.ResolutionToLower()));
            var destinationFolder = new DirectoryInfo(Path.Combine(
                Globals.DataFolder,
                SecurityType.Equity.SecurityTypeToLower(),
                Market.Borsdata,
                "fundamental",
                "coarse"));
            var fineFolder = new DirectoryInfo(Path.Combine(
                Globals.DataFolder,
                SecurityType.Equity.SecurityTypeToLower(),
                Market.Borsdata,
                "fundamental",
                "fine"));
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
                Market.Borsdata,
                blackListedTickersFile,
                reservedWordPrefix,
                mapFileProvider,
                factorFileProvider);
            return generator.Run();
        }
    }
}
