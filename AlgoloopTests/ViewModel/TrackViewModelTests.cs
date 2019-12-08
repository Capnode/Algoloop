/*
 * Copyright 2018 Capnode AB
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
using QuantConnect.Statistics;
using System;
using System.Collections.Generic;

namespace Algoloop.ViewModel.Tests
{
    [TestClass()]
    public class TrackViewModelTests
    {
        [TestMethod()]
        public void CalculateScoreTest_ideal()
        {
            var trades = new List<Trade>
            {
                new Trade{ ProfitLoss = 2, TotalFees = 1, EntryTime = new DateTime(2018,01,01), ExitTime = new DateTime(2018,02,01) },
                new Trade{ ProfitLoss = 2, TotalFees = 1, EntryTime = new DateTime(2018,03,01), ExitTime = new DateTime(2018,04,01) },
                new Trade{ ProfitLoss = 2, TotalFees = 1, EntryTime = new DateTime(2018,05,01), ExitTime = new DateTime(2018,06,01) },
                new Trade{ ProfitLoss = 2, TotalFees = 1, EntryTime = new DateTime(2018,07,01), ExitTime = new DateTime(2019,01,01) }
            };

            double score = TrackViewModel.CalculateScore(trades);
            Assert.IsTrue(score == 1);
        }

        [TestMethod()]
        public void CalculateScoreTest_example()
        {
            var trades = new List<Trade>
            {
                new Trade{ ProfitLoss = 5, TotalFees = 1, EntryTime = new DateTime(2018,01,01), ExitTime = new DateTime(2018,02,01) },
                new Trade{ ProfitLoss = 3, TotalFees = 1, EntryTime = new DateTime(2018,03,01), ExitTime = new DateTime(2018,04,01) },
                new Trade{ ProfitLoss = 2, TotalFees = 1, EntryTime = new DateTime(2018,05,01), ExitTime = new DateTime(2018,06,01) },
                new Trade{ ProfitLoss = 2, TotalFees = 1, EntryTime = new DateTime(2018,07,01), ExitTime = new DateTime(2019,01,01) }
            };

            double score = TrackViewModel.CalculateScore(trades);
            Assert.IsTrue(Math.Abs(score - 0.4726) < 0.0001);
        }
    }
}