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
using Algoloop.Provider;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Logging;
using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Algoloop.Tests.Provider
{
    [TestClass()]
    public class DukascopyTests
    {
        private SettingModel _settings;

        [TestInitialize()]
        public void Initialize()
        {
            _settings = new SettingModel
            {
                DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")
            };
        }

        [TestMethod()]
        public void Download_no_symbols()
        {
            var key = ConfigurationManager.AppSettings["dukascopy"];
            DateTime date = new DateTime(2019, 05, 01);
            var market = new MarketModel
            {
                Active = true,
                Name = "Dukascopy",
                Provider = "dukascopy",
                LastDate = date,
                Resolution = Resolution.Daily,
                ApiKey = key
            };

            // Just update symbol list
            MarketModel result = ProviderFactory.Download(market, _settings);
            Assert.IsFalse(result.Active);
            Assert.IsTrue(result.LastDate == date);
            Assert.AreEqual(78, market.Symbols.Count);
            Assert.AreEqual(0, market.Symbols.Where(m => m.Active).Count());
        }

        [TestMethod()]
        public void Download_one_symbol()
        {
            var key = ConfigurationManager.AppSettings["dukascopy"];
            DateTime date = new DateTime(2019, 05, 01);
            var market = new MarketModel
            {
                Active = true,
                Name = "Dukascopy",
                Provider = "dukascopy",
                LastDate = date,
                Resolution = Resolution.Daily,
                ApiKey = key
            };
            market.Symbols.Add(new SymbolModel("EURUSD", "Dukascopy", SecurityType.Forex));

            // Dwonload symbol and update list
            MarketModel result = ProviderFactory.Download(market, _settings);
            Assert.IsTrue(result.Active);
            Assert.IsTrue(result.LastDate > date);
            Assert.AreEqual(78, market.Symbols.Count);
            Assert.AreEqual(1, market.Symbols.Where(m => m.Active).Count());
        }

        [TestMethod()]
        public void Download_two_symbols()
        {
            var key = ConfigurationManager.AppSettings["dukascopy"];
            DateTime date = new DateTime(2019, 05, 01);
            var market = new MarketModel
            {
                Active = true,
                Name = "Dukascopy",
                Provider = "dukascopy",
                LastDate = date,
                Resolution = Resolution.Daily,
                ApiKey = key
            };
            market.Symbols.Add(new SymbolModel("EURUSD", "Dukascopy", SecurityType.Forex));
            market.Symbols.Add(new SymbolModel("GBPUSD", "Dukascopy", SecurityType.Forex));

            // Dwonload symbol and update list
            MarketModel result = ProviderFactory.Download(market, _settings);
            Assert.IsTrue(result.Active);
            Assert.IsTrue(result.LastDate > date);
            Assert.AreEqual(78, market.Symbols.Count);
            Assert.AreEqual(2, market.Symbols.Where(m => m.Active).Count());
        }

        [TestMethod()]
        public void Download_two_symbols_tick()
        {
            var key = ConfigurationManager.AppSettings["dukascopy"];
            DateTime date = new DateTime(2019, 05, 01);
            var market = new MarketModel
            {
                Active = true,
                Name = "Dukascopy",
                Provider = "dukascopy",
                LastDate = date,
                Resolution = Resolution.Tick,
                ApiKey = key
            };
            market.Symbols.Add(new SymbolModel("EURUSD", "Dukascopy", SecurityType.Forex));
            market.Symbols.Add(new SymbolModel("GBPUSD", "Dukascopy", SecurityType.Forex));

            // Dwonload symbol and update list
            MarketModel result = ProviderFactory.Download(market, _settings);
            Assert.IsTrue(result.Active);
            Assert.IsTrue(result.LastDate > date);
            Assert.AreEqual(78, market.Symbols.Count);
            Assert.AreEqual(2, market.Symbols.Where(m => m.Active).Count());
        }

        [TestMethod()]
        public void Download_invalid_symbol()
        {
            var key = ConfigurationManager.AppSettings["dukascopy"];
            DateTime date = new DateTime(2019, 05, 01);
            var market = new MarketModel
            {
                Active = true,
                Name = "Dukascopy",
                Provider = "dukascopy",
                LastDate = date,
                Resolution = Resolution.Daily,
                ApiKey = key
            };
            market.Symbols.Add(new SymbolModel("noname", "Dukascopy", SecurityType.Forex));

            // Dwonload symbol and update list
            MarketModel result = ProviderFactory.Download(market, _settings);
            Assert.IsFalse(result.Active);
            Assert.IsTrue(result.LastDate > date);
            Assert.AreEqual(79, market.Symbols.Count);
            Assert.AreEqual(1, market.Symbols.Where(m => m.Active).Count());
        }
    }
}