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
using Algoloop.Wpf.Provider;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Logging;
using System;
using System.IO;
using System.Linq;

namespace Algoloop.Tests.Provider
{
    [TestClass()]
    public class QuantconnectTests
    {
        private SettingModel _settings;

        [TestInitialize()]
        public void Initialize()
        {
            Log.LogHandler = new ConsoleLogHandler();

            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (Directory.Exists(dataFolder))
            {
                Directory.Delete(dataFolder, true);
            }

            _settings = new SettingModel { DataFolder = "Data" };
        }

        [TestMethod()]
        public void Download_no_symbols()
        {
            var date = new DateTime(2019, 05, 01);
            var market = new ProviderModel
            {
                Name = "QuantConnect",
                Provider = "quantconnect",
                LastDate = date
            };

            // Just update symbol list
            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            provider.GetMarketData(market);
            Assert.IsFalse(market.Active);
            Assert.IsTrue(market.LastDate == date);
            Assert.IsTrue(market.Symbols.Count > 42);
            Assert.AreEqual(market.Symbols.Count, market.Symbols.Where(m => m.Active).Count());
        }

        [TestMethod()]
        public void Download_one_symbol()
        {
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

            // Dwonload symbol and update list
            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            provider.GetMarketData(market);
            Assert.IsFalse(market.Active);
            Assert.IsTrue(market.LastDate > date);
            Assert.IsTrue(market.Symbols.Count > 42);
            Assert.AreEqual(market.Symbols.Count - 1, market.Symbols.Where(m => m.Active).Count());
        }
    }
}