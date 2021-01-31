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
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace Algoloop.Tests.Provider
{
    [TestClass]
    public class FxcmRestTests
    {
        private const string _provider = "fxcmrest";
        private SettingModel _settings;
        private ProviderModel _broker;

        [TestInitialize]
        public void Initialize()
        {
            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _settings = new SettingModel { DataFolder = dataFolder };

            _broker = new ProviderModel
            {
                Name = "FxcmRest",
                Provider = _provider,
                ApiKey = ConfigurationManager.AppSettings["fxcm_key"]
            };
        }

        [TestMethod]
        public void Login()
        {
            // Act
            using IProvider provider = ProviderFactory.CreateProvider(_broker.Provider, _settings);
            provider.Register(_settings, _broker.Provider);
            IReadOnlyList<AccountModel> accounts = provider.Login(_broker, _settings);

            // Assert
            Assert.IsTrue(accounts.Count > 0);
        }
    }
}
