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

using Algoloop.Model;
using Algoloop.Wpf.ViewModel;
using AlgoloopTests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Algoloop.Model.ProviderModel;

namespace Algoloop.Tests.ViewModel
{
    [TestClass()]
    public class MarketViewModelTests
    {
        private MarketsViewModel _markets;
        private ProviderModel _provider;
        private MarketViewModel _market;

        [TestInitialize()]
        public void Initialize()
        {
            Log.LogHandler = new ConsoleLogHandler();

            IConfigurationRoot config = TestConfig.Create();
            string access = config["fxcmrest-access"];
            AccessType accessType = (AccessType)Enum.Parse(typeof(AccessType), access);
            string key = config["fxcmrest-key"];

            var setting = new SettingModel();
            _markets = new MarketsViewModel(new MarketsModel(), setting);
            _provider = new ProviderModel
            {
                Provider = "fxcmrest",
                Access = accessType,
                ApiKey = key
            };
            _market = new MarketViewModel(_markets, _provider, setting);
            _markets.Markets.Add(_market);
        }

        [TestMethod()]
        public void DoStartCommand()
        {
            Log.Trace("MarketLoop");
            Task task =  _market.DoStartCommand();
            Thread.Sleep(5000);
            _market.DoStopCommand();
            task.Wait();

            Assert.IsTrue(task.IsCompleted);
            Log.Trace($"#Accounts: {_market.Model.Accounts.Count}");
            Assert.AreEqual(1, _market.Model.Accounts.Count);
            Log.Trace($"#Symbols: {_market.Model.Symbols.Count}");
            Assert.IsTrue(_market.Model.Symbols.Count > 0);
        }
    }
}
