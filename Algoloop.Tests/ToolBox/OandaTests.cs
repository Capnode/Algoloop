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
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Algoloop.Tests.ToolBox
{
    [TestClass, TestCategory("LocalTest")]
    public class OandaTests
    {
        private const string DataFolder = "Data";
        private const string TestData = "TestData";
        private const string MarketHours = "market-hours";
        private const string SymbolProperties = "symbol-properties";

        private string _forexFolder;

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
            _forexFolder = Path.Combine(DataFolder, SecurityType.Forex.SecurityTypeToLower(), Market.Oanda);

            // Settings
            IConfigurationRoot settings = TestConfig.Create();
            Config.Set("job-user-id", int.Parse(settings["job-user-id"]));
            Config.Set("api-access-token", settings["api-access-token"]);
            Config.Set("oanda-environment", settings["oanda-environment"]);
            Config.Set("oanda-access-token", settings["oanda-access-token"]);
            Config.Set("oanda-account-id", settings["oanda-account-id"]);
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public void Download_one_symbol()
        {
            // Arrange
            var tickers = new List<string> { "EURUSD" };
            var utcStart = new DateTime(2019, 05, 01, 0, 0, 0, DateTimeKind.Utc);
            var start = utcStart.ToLocalTime();
            var end = start.AddDays(1);
            string from = start.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string to = end.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
            string[] args =
            {
                "--app=OandaDownloader",
                $"--from-date={from}",
                $"--to-date={to}",
                $"--resolution=Daily",
                $"--tickers={string.Join(",", tickers)}"
            };

            // Act
            Program.Main(args);

            // Assert
            string datafile = Path.Combine(
                _forexFolder,
                nameof(Resolution.Daily),
                "EURUSD.zip");

            Assert.IsTrue(File.Exists(datafile));
        }
    }
}
