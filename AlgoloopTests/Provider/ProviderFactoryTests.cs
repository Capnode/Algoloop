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

using Algoloop.Lean;
using Algoloop.Model;
using Algoloop.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Algoloop.Provider.Tests
{
    [TestClass()]
    public class ProviderFactoryTests
    {
        private SettingService _settings;
        private HostDomainLogger _logger;
        private ProviderFactory _dut;

        [TestInitialize()]
        public void Initialize()
        {
            _settings = new SettingService
            {
                DataFolder = "Data"
            };

            _logger = new HostDomainLogger();

            _dut = new ProviderFactory();
        }

        [TestMethod()]
        public void Run_Dukascopy()
        {
            var market = new MarketModel
            {
                Name = "Dukascopy",
                Provider = "dukascopy"
            };

            MarketModel result = _dut.Run(market, _settings, _logger);
        }
    }
}