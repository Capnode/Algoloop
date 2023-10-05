/*
 * Copyright 2023 Capnode AB
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
using Algoloop.ViewModel.Internal.Provider;
using AlgoloopTests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Algoloop.Tests.Provider
{
    [TestClass]
    public class OandaTests
    {
        private const string DataFolder = "Data";
        private const string TestData = "TestData";
        private const string MarketHours = "market-hours";
        private const string SymbolProperties = "symbol-properties";

        private ProviderModel _model;

        [TestInitialize]
        public void Initialize()
        {
            // Make Debug.Assert break execution
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new DefaultTraceListener());
            Log.LogHandler = new ConsoleLogHandler();

            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataFolder);

            // Set Globals
            Config.Set("data-directory", dataFolder);
            Config.Set("data-folder", dataFolder);
            Config.Set("cache-location", dataFolder);
            Config.Set("version-id", string.Empty);
            Globals.Reset();

            // Remove temp dirs
            foreach (string dir in Directory.EnumerateDirectories(".", "temp*", SearchOption.TopDirectoryOnly))
            {
                Directory.Delete(dir, true);
            }

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

            IConfigurationRoot settings = TestConfig.Create();
            _model = new ProviderModel
            {
                Active = true,
                Name = "Oanda",
                Provider = Market.Oanda,
                Access = string.Compare(settings["oanda-environment"], "Trade", true) == 0 ?
                    ProviderModel.AccessType.Real : ProviderModel.AccessType.Demo,
                ApiKey = settings["oanda-access-token"],
                AccountId = settings["oanda-account-id"],
                LastDate = new DateTime(2023, 1, 4),
                Resolution = Resolution.Daily
            };

            // Settings
            Config.Set("job-user-id", int.Parse(settings["job-user-id"]));
            Config.Set("api-access-token", settings["api-access-token"]);
        }

        [TestMethod]
        public void GetUpdate_ListAllSymbols()
        {
            Log.Trace($"GetUpdate_ListAllSymbols");

            // Arrange
            DateTime lastDate = _model.LastDate;

            // Act
            using IProvider provider = ProviderFactory.CreateProvider(_model.Provider);
            provider.Login(_model);
            provider.GetUpdate(_model, null, CancellationToken.None);
            provider.Logout();
            var symbols = _model.Symbols;

            // Assert
            Assert.IsNotNull(symbols);
            Assert.IsTrue(symbols.Count > 0);
            Assert.IsTrue(_model.Active);
            Assert.IsTrue(_model.LastDate > lastDate);
        }

        [TestMethod]
        public void GetUpdate_AccountExist()
        {
            Log.Trace($"GetUpdate_OneSymbol");

            // Arrange
            DateTime lastDate = _model.LastDate;
            _model.Symbols.Add(new SymbolModel("EURUSD", Market.Oanda, SecurityType.Forex));

            // Act
            using IProvider provider = ProviderFactory.CreateProvider(_model.Provider);
            provider.Login(_model);
            provider.GetUpdate(_model, null, CancellationToken.None);
            provider.Logout();

            // Assert
            Assert.IsTrue(_model.Active);
            Assert.IsTrue(_model.Accounts.Count > 0);
        }

        [TestMethod]
        public void GetUpdate_OneSymbol()
        {
            Log.Trace($"GetUpdate_OneSymbol");

            // Arrange
            DateTime lastDate = _model.LastDate;
            _model.Symbols.Add(new SymbolModel("EURUSD", Market.Oanda, SecurityType.Forex));

            // Act
            using IProvider provider = ProviderFactory.CreateProvider(_model.Provider);
            provider.Login(_model);
            provider.GetUpdate(_model, null, CancellationToken.None);
            SymbolModel symbol = _model.Symbols.FirstOrDefault();
            provider.Logout();

            // Assert
            Assert.IsNotNull(symbol);
            Assert.IsTrue(_model.Active);
            Assert.IsTrue(_model.LastDate > lastDate);
            Assert.AreEqual(SecurityType.Forex, symbol.Security);
            Assert.AreEqual(Market.Oanda, symbol.Market);
            Assert.IsNotNull(symbol.Properties);
        }
    }
}
