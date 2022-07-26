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
using AlgoloopTests.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using static Algoloop.Model.ProviderModel;

namespace Algoloop.Tests.Provider
{
    [TestClass]
    public class FxcmTests
    {
        private SettingModel _settings;
        private ProviderModel _model;

        [TestInitialize]
        public void Initialize()
        {
            Log.LogHandler = new ConsoleLogHandler();

            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _settings = new SettingModel { DataFolder = dataFolder };
            var config = TestConfig.Create();
            string key = config["fxcm-key"];
            string access = config["fxcm-access"];

            _model = new ProviderModel
            {
                Name = "Fxcm",
                Provider = Market.FXCM,
                ApiKey = key,
                Access = (AccessType)Enum.Parse(typeof(AccessType), access),
            };
        }

        [TestMethod]
        public void Login()
        {
            // Act
            using IProvider provider = ProviderFactory.CreateProvider(_model.Provider, _settings);
            Assert.IsNotNull(provider);

            provider.Login(_model);
            provider.Logout();
        }

        [TestMethod()]
        public void SetUpdate()
        {
            int calls = 0;
            IReadOnlyList<SymbolModel> symbols = null;
            IReadOnlyList<QuoteBar> quotes = null;
            IReadOnlyList<AccountModel> accounts = null;

            // Just update symbol list
            using IProvider provider = ProviderFactory.CreateProvider(_model.Provider, _settings);
            provider.Login(_model);
            provider.SetUpdate(_model, list =>
            {
                calls++;
                if (list is IReadOnlyList<SymbolModel> symbolList)
                {
                    symbols = symbolList;
                }
                if (list is IReadOnlyList<QuoteBar> quoteList)
                {
                    quotes = quoteList;
                }
                if (list is IReadOnlyList<AccountModel> accountList)
                {
                    accounts = accountList;
                }
            });
            Thread.Sleep(6000);
            provider.Logout();

            Log.Trace($"calls={calls}");
            Log.Trace($"#symbols={symbols.Count}");
            Log.Trace($"#quotes={quotes.Count}");
            Log.Trace($"#accounts={accounts.Count}");

            Assert.IsTrue(calls >= 3);
            Assert.IsNotNull(symbols);
            Assert.IsNotNull(quotes);
            Assert.IsNotNull(accounts);
            Assert.AreEqual(1, accounts.Count);
            Assert.AreEqual(1, accounts[0].Balances.Count);
            Assert.IsTrue(_model.Active);
            Assert.IsTrue(_model.Symbols.Count > 200);
            Assert.IsTrue(_model.Symbols.Where(m => m.Active).Count() > 0);
        }
    }
}
