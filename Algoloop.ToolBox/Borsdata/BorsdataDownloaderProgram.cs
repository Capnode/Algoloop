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

using Newtonsoft.Json;
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
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Algoloop.ToolBox.Borsdata
{
    public static class BorsdataDownloaderProgram
    {
        private const SecurityType _securityType = SecurityType.Equity;
        private const string _market = "borsdata";
        private const string _reportInfoFile = "borsdata.csv";
        private const Resolution _resolution = Resolution.Daily;

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
                string dataDirectory = Config.Get("data-directory", "../../../Data");
                Register(dataDirectory, _market);
                Setup(dataDirectory);

                // Download the data
                using var downloader = new BorsdataDataDownloader(apiKey);
                IEnumerable<Symbol> symbols = tickers.Select(m => Symbol.Create(m, _securityType, _market));
                foreach (Symbol symbol in symbols)
                {
                    DateTime firstDate = DateTime.MaxValue;
                    IEnumerable<BaseData> data = downloader.Get(symbol, _resolution, startDate, endDate);
                    BaseData first = data?.FirstOrDefault();
                    if (first == default) continue;
                    if (first.Time < firstDate)
                    {
                        firstDate = first.Time.Date;
                    }
                    LeanDataWriter writer = new(_resolution, symbol, dataDirectory);
                    writer.Write(data);
                    UpdateMapFile(symbol.Value, _market, firstDate);
                }

                GenerateCoarseFiles(dataDirectory);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        public static void Register(string dataDirectory, string name)
        {
            if (!name.Equals(_market)) throw new ArgumentOutOfRangeException(nameof(name));

            // Set Market Hours
            string folder = Path.Combine(dataDirectory, "market-hours");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "market-hours-database.json");
            if (!File.Exists(path))
            {
                var emptyExchangeHours = new Dictionary<SecurityDatabaseKey, MarketHoursDatabase.Entry>();
                string jsonString = JsonConvert.SerializeObject(new MarketHoursDatabase(emptyExchangeHours));
                File.WriteAllText(path, jsonString);
            }

            MarketHoursDatabase marketHours = MarketHoursDatabase.FromDataFolder(dataDirectory);
            Debug.Assert(marketHours != null);

            var exchangeHours = new SecurityExchangeHours(
                TimeZones.Amsterdam,
                Enumerable.Empty<DateTime>(),
                new Dictionary<DayOfWeek, LocalMarketHours>
                {
                    { DayOfWeek.Monday, new LocalMarketHours(DayOfWeek.Monday, new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)) },
                    { DayOfWeek.Tuesday, new LocalMarketHours(DayOfWeek.Tuesday, new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)) },
                    { DayOfWeek.Wednesday, new LocalMarketHours(DayOfWeek.Wednesday, new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)) },
                    { DayOfWeek.Thursday, new LocalMarketHours(DayOfWeek.Thursday, new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)) },
                    { DayOfWeek.Friday, new LocalMarketHours(DayOfWeek.Friday, new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)) },
                    { DayOfWeek.Saturday, LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday) },
                    { DayOfWeek.Sunday, LocalMarketHours.ClosedAllDay(DayOfWeek.Sunday) }
                },
                new Dictionary<DateTime, TimeSpan>(),
                new Dictionary<DateTime, TimeSpan>());

            marketHours.SetEntry(
                name,
                null,
                SecurityType.Equity,
                exchangeHours,
                TimeZones.Amsterdam);

            // Register market
            if (Market.Encode(name) == null)
            {
                // be sure to add a reference to the unknown market, otherwise we won't be able to decode it coming out
                int code = 0;
                while (Market.Decode(code) != null)
                {
                    code++;
                }

                Market.Add(name, code);
            }
        }

        private static void Setup(string dataDirectory)
        {
            string equityFolder = Path.Combine(dataDirectory, nameof(SecurityType.Equity).ToLowerInvariant());
            Directory.CreateDirectory(equityFolder);

            // Create map_files filder
            string mapFilesFolder = Path.Combine(equityFolder, _market, "map_files");
            Directory.CreateDirectory(mapFilesFolder);

            // Copy Report info file
            string destPath = Path.Combine(equityFolder, _reportInfoFile);
            FileInfo destInfo = new(destPath);

            string srcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", _reportInfoFile);
            FileInfo srcInfo = new(srcPath);

            if (srcInfo.LastWriteTime > destInfo.LastWriteTime)
            {
                File.Copy(srcPath, destPath, true);
            }
        }

        private static void UpdateMapFile(string symbol, string market, DateTime date)
        {
            MapFile mapFile;
            IEnumerable<MapFileRow> presentRows;
            string mapRoot = MapFile.GetMapFilePath(market, _securityType);
            string path = Path.Combine(mapRoot, symbol.ToLowerInvariant() + ".csv");

            // Check if date is already mapped
            if (File.Exists(path))
            {
                IDataProvider dataProvider = Composer.Instance.GetExportedValueByTypeName<IDataProvider>(
                    Config.Get("data-provider", "DefaultDataProvider"));
                presentRows = MapFileRow.Read(path, market, _securityType, dataProvider);
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
            mapFile.WriteToCsv(market, _securityType);
        }

        private static bool GenerateCoarseFiles(string dataDirectory)
        {
            var dailyDataFolder = new DirectoryInfo(Path.Combine(
                dataDirectory, SecurityType.Equity.SecurityTypeToLower(),
                _market,
                Resolution.Daily.ResolutionToLower()));
            var destinationFolder = new DirectoryInfo(Path.Combine(
                dataDirectory,
                SecurityType.Equity.SecurityTypeToLower(),
                _market,
                "fundamental",
                "coarse"));
            var fineFolder = new DirectoryInfo(Path.Combine(
                dataDirectory,
                SecurityType.Equity.SecurityTypeToLower(),
                _market,
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
                _market,
                blackListedTickersFile,
                reservedWordPrefix,
                mapFileProvider,
                factorFileProvider);
            return generator.Run();
        }
    }
}
