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
using Algoloop.ToolBox;
using AlgoloopTests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Algoloop.Tests.ToolBox
{
    [TestClass, TestCategory("LocalTest")]
    public class BorsdataTests
    {
        private const string DataFolder = "Data";
        private const string TestData = "TestData";
        private const string MarketHours = "market-hours";
        private const string SymbolProperties = "symbol-properties";

        private string _equityFolder;
        private string _apiKey;

        [TestInitialize]
        public void Initialize()
        {
            // Make Debug.Assert break execution
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new DefaultTraceListener());
            Log.LogHandler = new ConsoleLogHandler();

            // Set Globals
            Config.Set("data-directory", DataFolder);
            Config.Set("data-folder", DataFolder);
            Config.Set("cache-location", DataFolder);
            Config.Set("version-id", string.Empty);
            Globals.Reset();

            // Prepare datafolder
            if (Directory.Exists(DataFolder))
            {
                Directory.Delete(DataFolder, true);
            }

            MainService.CopyDirectory(
                Path.Combine(TestData, MarketHours),
                Path.Combine(DataFolder, MarketHours));
            MainService.CopyDirectory(
                Path.Combine(TestData, SymbolProperties),
                Path.Combine(DataFolder, SymbolProperties));
            _equityFolder = Path.Combine(DataFolder, SecurityType.Equity.SecurityTypeToLower(), Market.Borsdata);

            // Settings
            IConfigurationRoot settings = TestConfig.Create();
            _apiKey = settings[Market.Borsdata];
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public void MainDownloadOneSymbol()
        {
            // Arrange
            string datafile = Path.Combine(
                _equityFolder,
                nameof(Resolution.Daily),
                "AXFO.ST.zip");
            string mapfile = Path.Combine(
                DataFolder,
                MapFile.GetRelativeMapFilePath(Market.Borsdata, SecurityType.Equity),
                "AXFO.ST.csv");
            
            var tickers = new List<string> { "AXFO.ST" };
            var start = new DateTime(2021, 01, 01, 20, 0, 0);
            var end = new DateTime(2021, 01, 10);
            string from = start.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string to = end.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string[] args =
            {
                "--app=BorsdataDownloader",
                $"--from-date={from}",
                $"--to-date={to}",
                $"--api-key={_apiKey}",
                $"--tickers={string.Join(",", tickers)}"
            };

            // Act
            Program.Main(args);

            // Assert
            Assert.IsTrue(File.Exists(datafile));
            Assert.IsTrue(File.Exists(mapfile));
        }

        [TestMethod]
        public void MainDownloadSymbolTwice()
        {
            var tickers = new List<string> { "AXFO.ST" };
            var start = new DateTime(2021, 01, 01, 20, 0, 0);
            var end = new DateTime(2021, 01, 10);
            string from = start.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string to = end.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string[] args =
            {
                "--app=BorsdataDownloader",
                $"--from-date={from}",
                $"--to-date={to}",
                $"--api-key={_apiKey}",
                $"--tickers={string.Join(",", tickers)}"
            };

            // Act
            Program.Main(args);

            // Assert
            string zipfile = Path.Combine(_equityFolder, "daily", "AXFO.ST.zip");
            string mapRoot = Path.Combine(
                DataFolder,
                MapFile.GetRelativeMapFilePath(Market.Borsdata, SecurityType.Equity));
            string mapPath = Path.Combine(mapRoot, "AXFO.ST.csv");
            Assert.IsTrue(File.Exists(zipfile));
            long length = new FileInfo(zipfile).Length;
            Assert.IsTrue(File.Exists(mapPath));
            string map = File.ReadAllText(mapPath);
            Assert.IsTrue(map.Length > 0);
            string firstDate = map[..8];

            var end2 = new DateTime(2021, 11, 15);
            string to2 = end2.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string[] args2 =
            {
                "--app=BorsdataDownloader",
                $"--from-date={from}",
                $"--to-date={to2}",
                $"--api-key={_apiKey}",
                $"--tickers={string.Join(",", tickers)}"
            };

            // Act
            Program.Main(args2);

            // Assert
            Assert.IsTrue(File.Exists(zipfile));
            long length2 = new FileInfo(zipfile).Length;
            Assert.IsTrue(length2 > length, "Append file failed");
            map = File.ReadAllText(mapPath);
            Assert.IsTrue(map.Length > 0);
            string firstDate2 = map[..8];
            Assert.AreEqual(firstDate, firstDate2);
        }

        [TestMethod]
        public void MainDownloadSymbolReload()
        {
            // Arrange
            var tickers = new List<string> { "AXFO.ST" };
            var start = new DateTime(2021, 01, 01, 20, 0, 0);
            var end = new DateTime(2021, 12, 9);
            string from = start.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string to = end.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string[] args =
            {
                "--app=BorsdataDownloader",
                $"--from-date={from}",
                $"--to-date={to}",
                $"--api-key={_apiKey}",
                $"--tickers={string.Join(",", tickers)}"
            };

            // Act
            Program.Main(args);

            // Assert
            string zipfile = Path.Combine(_equityFolder, "daily", "AXFO.ST.zip");
            Assert.IsTrue(File.Exists(zipfile));

            // Arrange
            var start2 = new DateTime(2021, 10, 05, 20, 0, 0);
            string from2 = start2.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string[] args2 =
            {
                "--app=BorsdataDownloader",
                $"--from-date={from2}",
                $"--to-date={to}",
                $"--api-key={_apiKey}",
                $"--tickers={string.Join(",", tickers)}"
            };

            // Invalidate TradeBars
            IEnumerable<Symbol> symbols = tickers.Select(m => Symbol.Create(m, SecurityType.Equity, Market.Borsdata));
            Symbol symbol = symbols.Single();
            List<BaseData> data = new();
            for (DateTime date = start2; date <= end; date = date.AddDays(1))
            {
                data.Add(new TradeBar(date, symbol, 0, 0, 0, 0, 0));
            }
            LeanDataWriter writer = new(Resolution.Daily, symbol, DataFolder);
            writer.Write(data);

            // Create invalid fundamentals
            string finezip1 = string.Format(CultureInfo.InvariantCulture, "{0:yyyyMMdd}.zip", start2);
            string fineFile1 = Path.Combine(_equityFolder, "fundamental", "fine", "axfo.st", finezip1);
            string finezip2 = string.Format(CultureInfo.InvariantCulture, "{0:yyyyMMdd}.zip", start2.AddDays(1));
            string fineFile2 = Path.Combine(_equityFolder, "fundamental", "fine", "axfo.st", finezip2);
            File.WriteAllText(fineFile1, "invalid");
            File.WriteAllText(fineFile2, "invalid");

            // Act
            Program.Main(args2);

            // Assert
            Assert.IsTrue(File.Exists(zipfile));
            long length2 = new FileInfo(zipfile).Length;
            Assert.AreNotEqual(0, length2);
            var leanDataReader = new QuantConnect.ToolBox.LeanDataReader(zipfile);
            data = leanDataReader.Parse().ToList();
            TradeBar startBar = data.Find(m => m.Time.Equals(start2)) as TradeBar;
            TradeBar endBar = data.Last() as TradeBar;
            Assert.AreEqual(start2, startBar.Time);
            Assert.AreEqual(0m, startBar.Close, "Start bar overwritten");
            Assert.AreEqual(end, endBar.Time);
            Assert.AreNotEqual(0m, endBar.Close, "End bar not replaced");
            Assert.IsTrue(File.Exists(fineFile1), "Invalid fine file removed");
            Assert.IsFalse(File.Exists(fineFile2), "Invalid fine file not removed");
        }

        [TestMethod]
        public void MainDownloadObsoleteSymbol()
        {
            // Arrange
            var tickers = new List<string> { "SWMA.ST" };
            var start = new DateTime(2021, 01, 01, 20, 0, 0);
            var end = new DateTime(2021, 12, 9);
            string from = start.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string to = end.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string[] args =
            {
                "--app=BorsdataDownloader",
                $"--from-date={from}",
                $"--to-date={to}",
                $"--api-key={_apiKey}",
                $"--tickers={string.Join(",", tickers)}"
            };

            // Act
            Program.Main(args);

            // Assert
            string zipfile = Path.Combine(_equityFolder, "daily", "SWMA.ST.zip");
            Assert.IsFalse(File.Exists(zipfile));
        }

        [TestMethod]
        public void MainDownloadAllHistory()
        {
            // Arrange
            var tickers = new List<string> { "AAK.ST" };
            var start = DateTime.MinValue;
            var end = new DateTime(2023, 4, 6);
            string from = start.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string to = end.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string[] args =
            {
                "--app=BorsdataDownloader",
                $"--from-date={from}",
                $"--to-date={to}",
                $"--api-key={_apiKey}",
                $"--tickers={string.Join(",", tickers)}"
            };

            // Act
            Program.Main(args);

            // Assert
            string zipfile = Path.Combine(_equityFolder, "daily", "AAK.ST.zip");
            Assert.IsTrue(File.Exists(zipfile));
        }

    }
}
