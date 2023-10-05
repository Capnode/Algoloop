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

using Algoloop.Model;
using Algoloop.ViewModel.Internal.Provider;
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
    [TestClass()]
    public class QuantconnectTests
    {
        private const string DataDirectory = "Data";

        [TestInitialize()]
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

            if (Directory.Exists(dataFolder))
            {
                Directory.Delete(dataFolder, true);
            }
        }

        [TestMethod()]
        public void GetUpdate_no_symbols()
        {
            // Arrange
            var date = new DateTime(2019, 05, 01);
            var market = new ProviderModel
            {
                Name = "QuantConnect",
                Provider = "quantconnect",
                LastDate = date
            };

            using IProvider provider = ProviderFactory.CreateProvider(market.Provider);

            // Act
            // Just update symbol list
            provider.GetUpdate(market, null, CancellationToken.None);

            // Assert
            Assert.IsFalse(market.Active);
            Assert.IsTrue(market.LastDate == date);
            Assert.AreEqual(100, market.Symbols.Count);
            Assert.AreEqual(market.Symbols.Count, market.Symbols.Where(m => m.Active).Count());
        }

        [TestMethod()]
        public void GetUpdate_one_symbol()
        {
            // Arrange
            var date = new DateTime(2019, 05, 01);
            var market = new ProviderModel
            {
                Name = "QuantConnect",
                Provider = "quantconnect",
                LastDate = date
            };
            market.Symbols.Add(new SymbolModel("aapl", "usa", SecurityType.Equity)
            {
                Active = false,
            });

            using IProvider provider = ProviderFactory.CreateProvider(market.Provider);

            // Act
            // Dwonload symbol and update list
            provider.GetUpdate(market, null, CancellationToken.None);

            // Assert
            Assert.IsFalse(market.Active);
            Assert.IsTrue(market.LastDate > date);
            Assert.AreEqual(100, market.Symbols.Count);
            Assert.AreEqual(market.Symbols.Count - 1, market.Symbols.Where(m => m.Active).Count());
        }
    }
}
