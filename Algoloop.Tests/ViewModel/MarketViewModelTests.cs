/*
 * Copyright 2021 Capnode AB
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
using Algoloop.ViewModel;
using AlgoloopTests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Algoloop.Model.ProviderModel;

namespace Algoloop.Tests.ViewModel
{
    [TestClass(), TestCategory("LocalTest")]
    public class MarketViewModelTests
    {
        private const string DataDirectory = "Data";
        
        private IConfigurationRoot _config;
        private SettingModel _setting;

        [TestInitialize()]
        public void Initialize()
        {
            Log.LogHandler = new ConsoleLogHandler();
            _config = TestConfig.Create();

            // Set Globals
            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataDirectory);
            Config.Set("data-directory", dataFolder);
            Config.Set("data-folder", dataFolder);
            Config.Set("cache-location", dataFolder);
            Config.Set("version-id", string.Empty);
            Globals.Reset();

            // Remove Data folder
            if (Directory.Exists(dataFolder))
            {
                Directory.Delete(dataFolder, true);
            }

            _setting = new SettingModel { DataFolder = dataFolder };
        }

        [TestMethod()]
        public void MarketLoop_Dukascopy()
        {
            // Arrange
            DateTime utcStart = new (2019, 05, 01, 0, 0, 0, DateTimeKind.Utc);
            DateTime date = utcStart.ToLocalTime();
            ProviderModel provider = new ()
            {
                Active = true,
                Name = "Dukascopy",
                Provider = Market.Dukascopy,
                LastDate = date,
                Resolution = Resolution.Daily
            };
            provider.Symbols.Add(new SymbolModel("EURUSD", Market.Dukascopy, SecurityType.Forex));
            MarketsViewModel markets = new(new MarketsModel(), _setting);
            MarketViewModel market = new (markets, provider, _setting);
            markets.Markets.Add(market);

            // Act
            Task task = market.DoStartCommand();
            Thread.Sleep(15000);
            market.DoStopCommand();
            task.Wait();
            Log.Trace($"#Accounts: {market.Model.Accounts.Count}");
            Log.Trace($"#Symbols: {market.Model.Symbols.Count}");

            // Assert
            Assert.IsTrue(task.IsCompleted);
            Assert.IsFalse(market.Active);
            Assert.AreNotEqual(date, provider.LastDate);
            Assert.AreEqual(78, market.Symbols.Count);
            Assert.AreEqual(1, market.Symbols.Where(m => m.Active).Count());
            string forexFolder = Path.Combine(_setting.DataFolder, SecurityType.Forex.SecurityTypeToLower(), Market.Dukascopy);
            Assert.IsTrue(File.Exists(Path.Combine(forexFolder, "daily", "eurusd.zip")));
            Assert.AreEqual(0, market.Model.Accounts.Count);
        }

        [TestMethod()]
        public void MarketLoop_Fxcm()
        {
            // Arrange
            string access = _config["fxcm-access"];
            AccessType accessType = (AccessType)Enum.Parse(typeof(AccessType), access);
            string key = _config["fxcm-key"];

            ProviderModel provider = new ()
            {
                Provider = Market.FXCM,
                Access = accessType,
                ApiKey = key
            };
            MarketsViewModel markets = new (new MarketsModel(), _setting);
            MarketViewModel market = new (markets, provider, _setting);
            markets.Markets.Add(market);

            // Act
            Task task =  market.DoStartCommand();
            Thread.Sleep(15000);
            market.DoStopCommand();
            task.Wait();
            Log.Trace($"#Accounts: {market.Model.Accounts.Count}");
            Log.Trace($"#Symbols: {market.Model.Symbols.Count}");

            // Assert
            Assert.IsTrue(task.IsCompleted);
            Assert.IsFalse(market.Active);
            Assert.AreEqual(1, market.Model.Accounts.Count);
            Assert.IsTrue(market.Model.Symbols.Count > 0);
        }
    }
}
