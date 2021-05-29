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
using Algoloop.Wpf.Provider;
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
        private string _forexFolder;

        [TestInitialize()]
        public void Initialize()
        {
            Log.LogHandler = new ConsoleLogHandler();

            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _forexFolder = Path.Combine(dataFolder, SecurityType.Forex.SecurityTypeToLower(), "dukascopy");

            if (Directory.Exists(dataFolder))
            {
                Directory.Delete(dataFolder, true);
            }

            _settings = new SettingModel { DataFolder = dataFolder };
        }

        [TestMethod()]
        public void Download_no_symbols()
        {
            var date = new DateTime(2019, 05, 01);
            var market = new ProviderModel
            {
                Active = true,
                Name = "Dukascopy",
                Provider = "dukascopy",
                LastDate = date,
                Resolution = Resolution.Daily
            };

            // Just update symbol list
            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            provider.GetMarketData(market);

            Assert.IsFalse(market.Active);
            Assert.AreEqual(date, market.LastDate);
            Assert.AreEqual(78, market.Symbols.Count);
            Assert.AreEqual(0, market.Symbols.Where(m => m.Active).Count());
        }

        [TestMethod()]
        public void Download_one_symbol()
        {
            var date = new DateTime(2019, 05, 01);
            var market = new ProviderModel
            {
                Active = true,
                Name = "Dukascopy",
                Provider = "dukascopy",
                LastDate = date,
                Resolution = Resolution.Daily
            };
            market.Symbols.Add(new SymbolModel("EURUSD", "Dukascopy", SecurityType.Forex));

            // Dwonload symbol and update list
            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            provider.GetMarketData(market);

            Assert.IsTrue(market.Active);
            Assert.AreEqual(date.AddDays(1), market.LastDate);
            Assert.AreEqual(78, market.Symbols.Count);
            Assert.AreEqual(1, market.Symbols.Where(m => m.Active).Count());
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "daily", "eurusd.zip")));
        }

        [TestMethod()]
        public void Download_two_symbols()
        {
            var date = new DateTime(2019, 05, 01);
            var market = new ProviderModel
            {
                Active = true,
                Name = "Dukascopy",
                Provider = "dukascopy",
                LastDate = date,
                Resolution = Resolution.Daily
            };
            market.Symbols.Add(new SymbolModel("EURUSD", "Dukascopy", SecurityType.Forex));
            market.Symbols.Add(new SymbolModel("GBPUSD", "Dukascopy", SecurityType.Forex));

            // Dwonload symbol and update list
            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            provider.GetMarketData(market);

            Assert.IsTrue(market.Active);
            Assert.AreEqual(date.AddDays(1), market.LastDate);
            Assert.AreEqual(78, market.Symbols.Count);
            Assert.AreEqual(2, market.Symbols.Where(m => m.Active).Count());
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "daily", "eurusd.zip")));
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "daily", "gbpusd.zip")));
        }

        [TestMethod()]
        public void Download_two_symbols_tick()
        {
            var date = new DateTime(2019, 05, 01);
            var market = new ProviderModel
            {
                Active = true,
                Name = "Dukascopy",
                Provider = "dukascopy",
                LastDate = date,
                Resolution = Resolution.Tick
            };
            market.Symbols.Add(new SymbolModel("EURUSD", "Dukascopy", SecurityType.Forex));
            market.Symbols.Add(new SymbolModel("GBPUSD", "Dukascopy", SecurityType.Forex));

            // Dwonload symbol and update list
            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            provider.GetMarketData(market);

            Assert.IsTrue(market.Active);
            Assert.AreEqual(date.AddDays(1), market.LastDate);
            Assert.AreEqual(78, market.Symbols.Count);
            Assert.AreEqual(2, market.Symbols.Where(m => m.Active).Count());
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "daily", "eurusd.zip")));
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "daily", "gbpusd.zip")));
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "hour", "eurusd.zip")));
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "hour", "gbpusd.zip")));
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "minute", "eurusd", "20190501_quote.zip")));
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "minute", "gbpusd", "20190501_quote.zip")));
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "second", "eurusd", "20190501_quote.zip")));
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "second", "gbpusd", "20190501_quote.zip")));
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "tick", "eurusd", "20190501_quote.zip")));
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, "tick", "gbpusd", "20190501_quote.zip")));
        }

        [TestMethod()]
        [ExpectedException(typeof(ApplicationException), "An invalid symbol name was accepted")]
        public void Download_invalid_symbol()
        {
            var date = new DateTime(2019, 05, 01);
            var market = new ProviderModel
            {
                Active = true,
                Name = "Dukascopy",
                Provider = "dukascopy",
                LastDate = date,
                Resolution = Resolution.Daily
            };
            market.Symbols.Add(new SymbolModel("noname", "Dukascopy", SecurityType.Forex));

            // Dwonload symbol and update list
            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            Assert.IsNotNull(provider);

            provider.GetMarketData(market);
            Assert.IsFalse(market.Active);
            Assert.AreEqual(market.LastDate, date);
            Assert.AreEqual(78, market.Symbols.Count);
            Assert.AreEqual(0, market.Symbols.Where(m => m.Active).Count());
        }

        [TestMethod()]
        public void Download_today()
        {
            DateTime today = DateTime.Today;
            var market = new ProviderModel
            {
                Active = true,
                Name = "Dukascopy",
                Provider = "dukascopy",
                LastDate = today,
                Resolution = Resolution.Second
            };
            market.Symbols.Add(new SymbolModel("EURUSD", "Dukascopy", SecurityType.Forex));

            // Dwonload symbol and update list
            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            provider.GetMarketData(market);

            Assert.IsFalse(market.Active);
            Assert.AreEqual(today, market.LastDate);
            Assert.AreEqual(78, market.Symbols.Count);
            Assert.AreEqual(1, market.Symbols.Where(m => m.Active).Count());
        }

        [TestMethod()]
        public void Download_yesterday()
        {
            DateTime today = DateTime.Today;
            var market = new ProviderModel
            {
                Active = true,
                Name = "Dukascopy",
                Provider = "dukascopy",
                LastDate = today.AddDays(-1),
                Resolution = Resolution.Second
            };
            market.Symbols.Add(new SymbolModel("EURUSD", "Dukascopy", SecurityType.Forex));

            // Dwonload symbol and update list
            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            provider.GetMarketData(market);

            Assert.IsTrue(market.Active);
            Assert.AreEqual(today, market.LastDate);
            Assert.AreEqual(78, market.Symbols.Count);
            Assert.AreEqual(1, market.Symbols.Where(m => m.Active).Count());
        }
    }
}
