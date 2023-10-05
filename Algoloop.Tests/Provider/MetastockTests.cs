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
using Algoloop.ViewModel.Internal.Provider;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Algoloop.Tests.Provider
{
    [TestClass]
    public class MetastockTests
    {
        private const string SourceDir = "TestData";
        private const string DestDir = "Data";
        
        private ProviderModel _market;
        private string _equityFolder;

        [TestInitialize]
        public void Initialize()
        {
            Log.LogHandler = new ConsoleLogHandler();

            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DestDir);

            // Set Globals
            Config.Set("data-directory", dataFolder);
            Config.Set("data-folder", dataFolder);
            Config.Set("cache-location", dataFolder);
            Config.Set("version-id", string.Empty);
            Globals.Reset();

            string sourceFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SourceDir);
            _equityFolder = Path.Combine(dataFolder, SecurityType.Equity.SecurityTypeToLower(), Market.Metastock);

            // Remove Data folder
            if (Directory.Exists(dataFolder))
            {
                Directory.Delete(dataFolder, true);
            }

            _market = new ProviderModel
            {
                Active = true,
                Name = "Metastock",
                Provider = Market.Metastock,
                SourceFolder = sourceFolder,
                LastDate = new DateTime(2021, 1, 5),
                Resolution = Resolution.Daily
            };
        }

        [TestMethod]
        public void GetUpdate()
        {
            // Arrange
            DateTime lastDate = _market.LastDate;

            // Act
            using IProvider provider = ProviderFactory.CreateProvider(_market.Provider);
            provider.GetUpdate(_market, null, CancellationToken.None);
            SymbolModel symbol = _market.Symbols.FirstOrDefault();

            // Assert
            Assert.IsNotNull(symbol);
            Assert.IsFalse(_market.Active);
            Assert.IsTrue(_market.LastDate > lastDate);
            Assert.AreEqual(SecurityType.Equity, symbol.Security);
            Assert.AreEqual(Market.Metastock, symbol.Market);
            Assert.IsNotNull(symbol.Properties);
            Assert.AreEqual(2, symbol.Properties.Count);
            Assert.IsTrue(File.Exists(Path.Combine(_equityFolder, "daily", "volvy.zip")));
        }
    }
}
