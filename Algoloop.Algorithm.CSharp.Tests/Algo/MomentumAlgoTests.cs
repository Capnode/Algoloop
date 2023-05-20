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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using QuantConnect;
using QuantConnect.Logging;

namespace Algoloop.Algorithm.CSharp.Algo.Tests
{
    [TestClass()]
    public class MomentumAlgoTests
    {
        private const string AlgorithmLocation = "Algoloop.Algorithm.CSharp.dll";

        [TestInitialize]
        public void Initialize()
        {
            Log.LogHandler = new ConsoleLogHandler();
            Log.DebuggingEnabled = false;
        }

        [TestMethod]
        public void Trade()
        {
            Dictionary<string, string> result = TestEngine.Run(
                "MomentumAlgo",
                AlgorithmLocation,
                DateTime.Parse("2018-01-01 00:00:00", CultureInfo.InvariantCulture),
                DateTime.Parse("2018-12-31 23:59:59", CultureInfo.InvariantCulture),
                10000,
                new Dictionary<string, string>
                {
                    { "resolution", "Daily" },
                    { "security", "Equity" },
                    { "market", Market.Borsdata },
                    { "symbols", "ABB.ST"},
                    { "Period", "100" },
                    { "Hold", "5" }
                });

            result.ToList().ForEach(m => Console.Out.WriteLine($"{m.Key}={m.Value}"));
            Assert.IsTrue(result.TryGetValue("Total Trades", out string trades));
            Assert.IsTrue(int.TryParse(trades, out int trade));
            Logger.LogMessage($"trade={trade}");
            Assert.AreEqual(4, trade);
        }

        [TestMethod]
        public void Trade_with_Tracker_Portfolio()
        {
            Dictionary<string, string> result = TestEngine.Run(
                "MomentumAlgo",
                AlgorithmLocation,
                DateTime.Parse("2021-01-01 00:00:00", CultureInfo.InvariantCulture),
                DateTime.Parse("2021-12-31 23:59:59", CultureInfo.InvariantCulture),
                10000,
                new Dictionary<string, string>
                {
                    { "resolution", "Daily" },
                    { "security", "Equity" },
                    { "market", Market.Borsdata },
                    { "symbols", "ABB.ST;ERIC-B.ST;ATCO-A.ST;SEB-A.ST"},
                    { "Period", "100" },
                    { "Hold", "1" },
                    { "Slots", "2" },
                    { "Rebalance trigger (min)", "0.7" },
                    { "Tracker stoploss period", "10" },
                    { "Stoploss sizing", "0" },
                });

            result.ToList().ForEach(m => Console.Out.WriteLine($"{m.Key}={m.Value}"));
            Assert.IsTrue(result.TryGetValue("Total Trades", out string trades));
            Assert.IsTrue(int.TryParse(trades, out int trade));
            Logger.LogMessage($"trade={trade}");
            Assert.AreEqual(106, trade);
        }
    }
}
