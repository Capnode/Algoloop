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
using Algoloop.ViewModel.Internal.Provider;
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

namespace Algoloop.Tests.Provider
{
    [TestClass, TestCategory("LocalTest")]
    public class BorsdataTests
    {
        private const string DataDirectory = "Data";

        private ProviderModel _market;
        private string _equityFolder;

        [TestInitialize]
        public void Initialize()
        {
            Log.LogHandler = new ConsoleLogHandler();

            // Set Globals
            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataDirectory);
            Config.Set("data-directory", dataFolder);
            Config.Set("data-folder", dataFolder);
            Config.Set("cache-location", dataFolder);
            Config.Set("version-id", string.Empty);
            Globals.Reset();

            _equityFolder = Path.Combine(dataFolder, SecurityType.Equity.SecurityTypeToLower(), Market.Borsdata);
            if (Directory.Exists(dataFolder))
            {
                Directory.Delete(dataFolder, true);
            }

            IConfigurationRoot settings = TestConfig.Create();
            _market = new ProviderModel
            {
                Active = true,
                Name = "Borsdata",
                Provider = Market.Borsdata,
                ApiKey = settings[Market.Borsdata],
                LastDate = new DateTime(2021, 1, 5),
                Resolution = Resolution.Daily
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public void GetUpdateOneSymbol()
        {
            // Arrange
            DateTime lastDate = _market.LastDate;
            const string ticker = "INVE-B.ST";
            var symbol0 = new SymbolModel(ticker, string.Empty, SecurityType.Base)
            {
                Active = false,
                Name = ticker
            };
            _market.Symbols.Add(symbol0);

            // Act
            using IProvider provider = ProviderFactory.CreateProvider(_market.Provider);
            provider.GetUpdate(_market, null, CancellationToken.None);
            SymbolModel symbol = _market.Symbols.SingleOrDefault();

            // Assert
            Assert.IsNotNull(symbol);
            Assert.AreSame(symbol0, symbol);
            Assert.IsFalse(_market.Active);
            Assert.IsTrue(_market.LastDate > lastDate);
            Assert.AreEqual(symbol0.Id, symbol.Id);
            Assert.AreNotEqual(symbol.Id, symbol.Name);
            Assert.AreEqual(symbol0.Active, symbol.Active);
            Assert.AreEqual("Investor B", symbol.Name);
            Assert.AreEqual(SecurityType.Equity, symbol.Security);
            Assert.AreEqual(Market.Borsdata, symbol.Market);
            Assert.IsNotNull(symbol.Properties);
            Assert.AreEqual(5, symbol.Properties.Count);
            Assert.IsTrue(File.Exists(Path.Combine(_equityFolder, "daily", "inve-b.st.zip")));
        }

        [TestMethod]
        public void GetUpdateRemoveObsoleteSymbol()
        {
            // Arrange
            DateTime lastDate = _market.LastDate;
            const string ticker = "SWMA.ST";
            var symbol0 = new SymbolModel(ticker, string.Empty, SecurityType.Base)
            {
                Active = false,
                Name = ticker
            };
            _market.Symbols.Add(symbol0);

            // Act
            using IProvider provider = ProviderFactory.CreateProvider(_market.Provider);
            provider.GetUpdate(_market, null, CancellationToken.None);

            // Assert
            Assert.IsNotNull(_market.Symbols);
            Assert.IsFalse(_market.Active);
            Assert.IsTrue(_market.LastDate == lastDate);
            Assert.IsFalse(File.Exists(Path.Combine(_equityFolder, "daily", "swma.st.zip")));
        }
    }
}
