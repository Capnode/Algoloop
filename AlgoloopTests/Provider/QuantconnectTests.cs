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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Logging;
using System;
using System.Linq;

namespace Algoloop.Provider.Tests
{
    [TestClass()]
    public class QuantconnectTests
    {
        private SettingModel _settings;
        private ProviderFactory _dut;

        [TestInitialize()]
        public void Initialize()
        {
            _settings = new SettingModel
            {
                DataFolder = "Data"
            };

            _dut = new ProviderFactory();
        }

        [TestMethod()]
        public void Download_no_symbols()
        {
            DateTime date = new DateTime(2019, 05, 01);
            var market = new MarketModel
            {
                Name = "QuantConnect",
                Provider = "quantconnect",
                LastDate = date
            };

            // Just update symbol list
            MarketModel result = _dut.Download(market, _settings, Log.LogHandler);
            Assert.IsFalse(result.Active);
            Assert.IsTrue(result.LastDate == date);
            Assert.AreEqual(42, market.Symbols.Count);
            Assert.AreEqual(market.Symbols.Count, market.Symbols.Where(m => m.Active).Count());
        }

        [TestMethod()]
        public void Download_one_symbol()
        {
            DateTime date = new DateTime(2019, 05, 01);
            var market = new MarketModel
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
            MarketModel result = _dut.Download(market, _settings, Log.LogHandler);
            Assert.IsFalse(result.Active);
            Assert.IsTrue(result.LastDate > date);
            Assert.AreEqual(42, market.Symbols.Count);
            Assert.AreEqual(market.Symbols.Count - 1, market.Symbols.Where(m => m.Active).Count());
        }
    }
}