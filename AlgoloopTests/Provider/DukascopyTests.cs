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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Algoloop.Provider.Tests
{
    [TestClass()]
    public class DukascopyTests
    {
        private MarketModel _model;
        private Dukascopy _dut;

        [TestInitialize()]
        public void Initialize()
        {
            _model = new MarketModel
            {
                Name = "Dukascopy"
            };

            _dut = new Dukascopy();
        }

        [TestMethod()]
        public void DownloadTest()
        {
        }

        [TestMethod()]
        public void GetAllSymbolsTest()
        {
            IEnumerable<SymbolModel> symbols = _dut.GetAllSymbols(_model);
            Assert.AreEqual(78, symbols.ToList().Count());
        }
    }
}