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
using Algoloop.Provider;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Configuration;
using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Algoloop.Tests.Provider
{
    [TestClass()]
    public class FxcmTests
    {
        private SettingModel _settings;

        [TestInitialize()]
        public void Initialize()
        {
            Config.Set("map-file-provider", "LocalDiskMapFileProvider");
            
            _settings = new SettingModel
            {
                DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")
            };
        }

        [TestMethod()]
        public void Download_no_symbols()
        {
            string terminal = ConfigurationManager.AppSettings["fxcm_terminal"];
            string user = ConfigurationManager.AppSettings["fxcm_user"];
            string pass = ConfigurationManager.AppSettings["fxcm_pass"];
            DateTime date = new DateTime(2019, 05, 01);
            var market = new MarketModel
            {
                Active = true,
                Name = "Fxcm",
                Provider = "fxcm",
                LastDate = date,
                Resolution = Resolution.Daily,
                Access = (MarketModel.AccessType)Enum.Parse(typeof(MarketModel.AccessType), terminal),
                Login = user,
                Password = pass
            };

            // Just update symbol list
            using IProvider provider = ProviderFactory.CreateProvider(market, _settings);
            provider.Download(market, _settings);
            Assert.IsFalse(market.Active);
            Assert.IsTrue(market.LastDate == date);
            Assert.AreEqual(75, market.Symbols.Count);
            Assert.AreEqual(0, market.Symbols.Where(m => m.Active).Count());
        }

        [DataRow(Resolution.Daily)]
        [DataRow(Resolution.Hour)]
        [DataRow(Resolution.Minute)]
        [DataRow(Resolution.Second)]
//        [DataRow(Resolution.Tick)]
        [DataTestMethod]
        public void Download_one_symbol(Resolution resolution)
        {
            string terminal = ConfigurationManager.AppSettings["fxcm_terminal"];
            string user = ConfigurationManager.AppSettings["fxcm_user"];
            string pass = ConfigurationManager.AppSettings["fxcm_pass"];
            DateTime date = new DateTime(2019, 05, 01);
            var market = new MarketModel
            {
                Active = true,
                Name = "Fxcm",
                Provider = "fxcm",
                LastDate = date,
                Resolution = resolution,
                Access = (MarketModel.AccessType)Enum.Parse(typeof(MarketModel.AccessType), terminal),
                Login = user,
                Password = pass
            };
            market.Symbols.Add(new SymbolModel("EURUSD", "fxcm", SecurityType.Forex));

            // Dwonload symbol and update list
            using IProvider provider = ProviderFactory.CreateProvider(market, _settings);
            provider.Download(market, _settings);
            Assert.IsTrue(market.LastDate > date);
            Assert.AreEqual(75, market.Symbols.Count);
            Assert.AreEqual(1, market.Symbols.Where(m => m.Active).Count());
        }

    }
}
