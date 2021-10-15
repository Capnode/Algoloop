/*
 * Copyright 2021 Capnode AB
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

using Algoloop.Brokerages.FxcmRest;
using Algoloop.Model;
using AlgoloopTests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Algoloop.Model.ProviderModel;

namespace Algoloop.Tests.Brokerages
{
    [TestClass]
    public class FxcmRestTests : IDisposable
    {
        const string _market = "fxcmrest";
        private FxcmClient _api;
        private bool disposedValue;

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Add a reference to the unknown market
            int code = 0;
            while (Market.Decode(code) != null)
            {
                code++;
            }

            Market.Add(_market, code);
        }

        [ClassCleanup]
        public static void Teardown()
        {
        }

        [TestInitialize]
        public void Initialize()
        {
            Log.LogHandler = new ConsoleLogHandler();

            IConfigurationRoot config = TestConfig.Create();
            string access = config["fxcmrest-access"];
            AccessType accessType = (AccessType)Enum.Parse(typeof(AccessType), access);
            string key = config["fxcmrest-key"];
            _api = new FxcmClient(accessType, key);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _api.Dispose();
        }

        [TestMethod]
        public void LoginAsync()
        {
            // Act
            _api.Login();
            _api.Logout();
        }

        [TestMethod]
        public async Task GetAccountsAsync()
        {
            // Act
            _api.Login();
            IReadOnlyList<AccountModel> accounts = null;
            await _api.GetAccountsAsync(acct => accounts = acct as IReadOnlyList<AccountModel>)
                .ConfigureAwait(false);
            _api.Logout();

            Assert.IsNotNull(accounts);
            Assert.AreEqual(1, accounts.Count);
            Assert.AreEqual(1, accounts[0].Balances.Count);
        }

        [TestMethod]
        public async Task GetSymbolsAsync()
        {
            // Act
            _api.Login();
            IReadOnlyList<SymbolModel> symbols = await _api.GetSymbolsAsync()
                .ConfigureAwait(false);
            _api.Logout();

            Assert.IsNotNull(symbols);
            Assert.AreNotEqual(0, symbols.Count);
        }

        [TestMethod]
        public async Task GetMarketData()
        {
            // Act
            _api.Login();
            IReadOnlyList<QuoteBar> list = null;
            await _api.GetMarketDataAsync(m =>
            {
                if (m is IReadOnlyList<QuoteBar> quotes)
                {
                    list = quotes;
                }
            }).ConfigureAwait(false);
            Thread.Sleep(6000);
            _api.Logout();

            Assert.IsNotNull(list);
            Assert.AreNotEqual(0, list.Count);
        }

        [TestMethod]
        public async Task SubscribeMarketData()
        {
            List<SymbolModel> symbols = new () { new SymbolModel("EUR/USD", "fxcm", SecurityType.Cfd) };

            // Act
            _api.Login();
            IReadOnlyList<QuoteBar> list = null;
            await _api.SubscribeMarketDataAsync(symbols, m =>
            {
                if (m is IReadOnlyList<QuoteBar> quotes)
                {
                    list = quotes;
                }
            }).ConfigureAwait(false);
            Thread.Sleep(6000);
            Assert.IsNotNull(list);
            Assert.AreNotEqual(0, list.Count);

            await _api.UnsubscribeQuotesAsync(symbols).ConfigureAwait(false);
            list = null;
            Thread.Sleep(6000);
            Assert.IsNull(list);
            _api.Logout();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _api.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
