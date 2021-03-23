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
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Algoloop.Tests.Provider
{
    [TestClass]
    public class FxcmTests
    {
        private SettingModel _settings;
        private string _forexFolder;
        private ProviderModel.AccessType _access;
        private string _user;
        private string _pass;
        private string _account;

        [TestInitialize]
        public void Initialize()
        {
            Log.LogHandler = new ConsoleLogHandler();

            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _forexFolder = Path.Combine(dataFolder, SecurityType.Forex.SecurityTypeToLower(), "fxcm");
            if (Directory.Exists(dataFolder))
            {
                Directory.Delete(dataFolder, true);
            }

            Config.Set("map-file-provider", "LocalDiskMapFileProvider");
            _settings = new SettingModel { DataFolder = dataFolder };

            string terminal = ConfigurationManager.AppSettings["fxcm-terminal"];
            _access = (ProviderModel.AccessType)Enum.Parse(typeof(ProviderModel.AccessType), terminal);
            _user = ConfigurationManager.AppSettings["fxcm-user-name"];
            _pass = ConfigurationManager.AppSettings["fxcm-password"];
            _account = ConfigurationManager.AppSettings["fxcm-account-id"];
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException), "An invalid symbol name was accepted")]
        public void Download_no_symbols()
        {
            var date = new DateTime(2019, 05, 01);
            var market = new ProviderModel
            {
                Active = true,
                Name = "Fxcm",
                Provider = "fxcm",
                LastDate = date,
                Resolution = Resolution.Daily,
                Access = _access,
                Login = _user,
                Password = _pass
            };

            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            provider.GetMarketData(market);

            Assert.IsFalse(market.Active);
            Assert.AreEqual(date, market.LastDate);
            Assert.AreEqual(75, market.Symbols.Count);
            Assert.AreEqual(0, market.Symbols.Where(m => m.Active).Count());
        }

        [DataRow(Resolution.Daily, "eurusd.zip")]
        [DataRow(Resolution.Hour, "eurusd.zip")]
        [DataRow(Resolution.Minute, "eurusd//20190501_quote.zip")]
        [DataRow(Resolution.Second, "eurusd//20190501_quote.zip")]
//        [DataRow(Resolution.Tick)]
        [DataTestMethod]
        public void Download_one_symbol(Resolution resolution, string filename)
        {
            var date = new DateTime(2019, 05, 01);
            DateTime nextDay = date.AddDays(1);
            var market = new ProviderModel
            {
                Active = true,
                Name = "Fxcm",
                Provider = "fxcm",
                LastDate = date,
                Resolution = resolution,
                Access = _access,
                Login = _user,
                Password = _pass
            };
            market.Symbols.Add(new SymbolModel("EURUSD", "fxcm", SecurityType.Forex));

            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            provider.GetMarketData(market);

            Assert.IsTrue(market.Active);
            Assert.AreEqual(nextDay, market.LastDate);
            Assert.AreEqual(75, market.Symbols.Count);
            Assert.AreEqual(1, market.Symbols.Where(m => m.Active).Count());
            Assert.IsTrue(File.Exists(Path.Combine(_forexFolder, resolution.ResolutionToLower(), filename)));
        }

        [TestMethod]
        public void Download_yesterday()
        {
            DateTime today = DateTime.Today;
            DateTime yesterday = today.AddDays(-1);
            var market = new ProviderModel
            {
                Active = true,
                Name = "Fxcm",
                Provider = "fxcm",
                LastDate = yesterday,
                Resolution = Resolution.Minute,
                Access = _access,
                Login = _user,
                Password = _pass
            };
            market.Symbols.Add(new SymbolModel("EURUSD", "fxcm", SecurityType.Forex));

            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            provider.GetMarketData(market);

            Assert.IsTrue(market.Active);
            Assert.AreEqual(today, market.LastDate);
            Assert.AreEqual(75, market.Symbols.Count);
            Assert.AreEqual(1, market.Symbols.Where(m => m.Active).Count());
        }

        [TestMethod]
        public void Download_today()
        {
            DateTime today = DateTime.Today;
            var market = new ProviderModel
            {
                Active = true,
                Name = "Fxcm",
                Provider = "fxcm",
                LastDate = today,
                Resolution = Resolution.Minute,
                Access = _access,
                Login = _user,
                Password = _pass
            };
            market.Symbols.Add(new SymbolModel("EURUSD", "fxcm", SecurityType.Forex));

            using IProvider provider = ProviderFactory.CreateProvider(market.Provider, _settings);
            provider.GetMarketData(market);

            Assert.IsFalse(market.Active);
            Assert.AreEqual(today, market.LastDate);
            Assert.AreEqual(75, market.Symbols.Count);
            Assert.AreEqual(1, market.Symbols.Where(m => m.Active).Count());
        }

        [Ignore]
        [TestMethod]
        public void Login()
        {
            var broker = new ProviderModel
            {
                Active = true,
                Name = "Fxcm",
                Provider = "fxcm",
                Access = _access,
                Login = _user,
                Password = _pass
            };

            using IProvider provider = ProviderFactory.CreateProvider(broker.Provider, _settings);
            provider.Login(broker);

            Assert.IsTrue(broker.Active);
        }

    }
}
