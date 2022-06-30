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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using QuantConnect.Logging;

namespace Algoloop.Algorithm.CSharp.Algo.Tests
{
    [TestClass()]
    public class LogQuotesTests
    {
        [TestInitialize]
        public void Initialize()
        {
            Log.LogHandler = new ConsoleLogHandler();
        }

        [TestMethod]
        public void Trade()
        {
            Dictionary<string, string> result = TestEngine.Run(
                "LogQuotesAlgo",
                DateTime.Parse("2018-01-01 00:00:00", CultureInfo.InvariantCulture),
                DateTime.Parse("2018-12-31 23:59:59", CultureInfo.InvariantCulture),
                10000,
                new Dictionary<string, string>
                {
                    { "resolution", "Hour" },
                    { "market", "dukascopy" },
                    { "symbols", "EURUSD"}
                });

            result.ToList().ForEach(m => Console.Out.WriteLine($"{m.Key}={m.Value}"));
        }
    }
}
