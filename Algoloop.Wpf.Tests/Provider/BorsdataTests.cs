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

using Accord.Math;
using Algoloop.Model;
using Algoloop.Wpf.ViewModels.Internal.Provider;
using AlgoloopTests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Algoloop.Wpf.Tests.Provider
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
                Resolution = Resolution.Daily
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public void GetUpdate_OneSymbol_ExpectSuccess()
        {
            // Arrange
            using IProvider provider = ProviderFactory.CreateProvider(_market.Provider);

            _market.LastDate = DateTime.Today.AddDays(1);
            provider.GetUpdate(_market, null, CancellationToken.None);
            _market.Symbols.Apply(m => m.Active = false);
            var symbol0 = _market.Symbols.Single(m => m.Id == "INVE-B.ST");
            symbol0.Active = true;
            _market.LastDate = new DateTime(2021, 1, 5);
            var lastDate = _market.LastDate;

            // Act
            provider.GetUpdate(_market, null, CancellationToken.None);
            SymbolModel symbol = _market.Symbols.Single(m => m.Id == "INVE-B.ST");

            // Assert
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
        public void GetUpdate_Twice_ExpectListsUnchanged()
        {
            // Arrange1
            DateTime startDate = new DateTime(2021, 1, 5);
            using IProvider provider = ProviderFactory.CreateProvider(_market.Provider);

            _market.LastDate = DateTime.Today.AddDays(1);
            provider.GetUpdate(_market, null, CancellationToken.None);
            _market.Symbols.Apply(m => m.Active = false);
            var symbol0 = _market.Symbols.Single(m => m.Id == "INVE-B.ST");
            symbol0.Active = true;
            _market.LastDate = startDate;
            var lastDate = _market.LastDate;

            // Act
            provider.GetUpdate(_market, null, CancellationToken.None);
            var lists1 = _market.Lists.ToList();
            _market.LastDate = startDate;
            provider.GetUpdate(_market, null, CancellationToken.None);
            var lists2 = _market.Lists.ToList();

            // Assert
            var print1 = PrintLists(lists1);
            var print2 = PrintLists(lists2);
            Assert.AreEqual(print1, print2);
        }

        [TestMethod]
        public void GetUpdate_RemoveObsoleteSymbol()
        {
            // Arrange
            DateTime startDate = new DateTime(2021, 1, 5);
            using IProvider provider = ProviderFactory.CreateProvider(_market.Provider);
            _market.LastDate = DateTime.Today.AddDays(1);
            provider.GetUpdate(_market, null, CancellationToken.None);
            _market.Symbols.Apply(m => m.Active = false);
            _market.Symbols.Add(new SymbolModel("SWMA.ST", string.Empty, SecurityType.Base));
            _market.LastDate = startDate;

            // Act
            provider.GetUpdate(_market, null, CancellationToken.None);

            // Assert
            Assert.IsNotNull(_market.Symbols);
            Assert.IsFalse(_market.Active);
            Assert.IsTrue(_market.LastDate == startDate);
            Assert.IsFalse(_market.Symbols.Any(m => m.Id == "SWMA.ST"));
            Assert.IsFalse(File.Exists(Path.Combine(_equityFolder, "daily", "swma.st.zip")));
        }

        [TestMethod]
        public void UpdateSymbols_ListChange()
        {
            // Arrange
            var symbol = new SymbolModel()
            {
                Id = "1",
                Active = true,
                Market = Market.Borsdata,
                Name = "ERICB.ST",
                Security = SecurityType.Equity,
                Properties = { { "Country", "Sweden" }, { "Marketplace", "Mid Cap" } },
            };

            var list = new ListModel("Mid Cap Sweden")
            {
                Symbols = { symbol },
                Auto = true
            };

            _market.Symbols.Add(symbol);
            _market.Lists.Add(list);

            var changedSymbol = new SymbolModel()
            {
                Id = "1",
                Active = true,
                Market = Market.Borsdata,
                Name = "ERICB.ST",
                Security = SecurityType.Equity,
                Properties = { { "Country", "Sweden" }, { "Marketplace", "Large Cap" } },
            };

            var actual = new List<SymbolModel>() { changedSymbol };

            // Act
            Algoloop.Wpf.ViewModels.Internal.Provider.Borsdata.UpdateSymbols(_market, actual, null);

            // Assert
            Assert.AreEqual(1, _market.Symbols.Count);
            Assert.AreEqual(2, _market.Lists.Count);

            var swedenList = _market.Lists.FirstOrDefault(x => x.Name == "Sweden");
            Assert.IsNotNull(swedenList);
            Assert.AreEqual(1, swedenList.Symbols.Count);

            var largeCapList = _market.Lists.FirstOrDefault(x => x.Name == "Large Cap Sweden");
            Assert.IsNotNull(largeCapList);
            Assert.AreEqual(1, largeCapList.Symbols.Count);
        }

        private string PrintLists(List<ListModel> lists)
        {
            var builder = new System.Text.StringBuilder();
            foreach (var list in lists)
            {
                builder.AppendLine(list.Name);
                foreach (var symbol in list.Symbols)
                {
                    builder.AppendLine($"  {symbol.Name}");
                }
            }

            return builder.ToString();
        }
    }
}
