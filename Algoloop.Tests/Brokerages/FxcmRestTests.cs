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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using static Algoloop.Model.ProviderModel;

namespace Algoloop.Tests.Brokerages
{
    [TestClass]
    public class FxcmRestTests : IDisposable
    {
        const string _market = "fxcm_rest";
        private FxcmClient _api;
        private bool disposedValue;

        [ClassInitialize]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "context")]
        public static void Setup(TestContext context)
        {
            Market.Add(_market, 100);
        }

        [ClassCleanup]
        public static void Teardown()
        {
        }

        [TestInitialize]
        public void Initialize()
        {
            string access = ConfigurationManager.AppSettings["fxcm_access"];
            AccessType accessType = (AccessType)Enum.Parse(typeof(AccessType), access);

            string key = ConfigurationManager.AppSettings["fxcm_key"];
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
            bool connected = _api.LoginAsync();
            bool disconnected = _api.LogoutAsync();

            Assert.IsTrue(connected);
            Assert.IsTrue(disconnected);
        }

        //[TestMethod]
        //public async Task GetAccountsAsync()
        //{
        //    // Act
        //    bool connected = await _api.LoginAsync().ConfigureAwait(false);
        //    IReadOnlyList<AccountModel> accounts = await _api.GetAccountsAsync().ConfigureAwait(true);
        //    bool disconnected = await _api.LogoutAsync().ConfigureAwait(true);

        //    Assert.IsTrue(connected);
        //    Assert.IsTrue(disconnected);
        //    Assert.IsNotNull(accounts);
        //    Assert.IsTrue(accounts.Count > 0);
        //}

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