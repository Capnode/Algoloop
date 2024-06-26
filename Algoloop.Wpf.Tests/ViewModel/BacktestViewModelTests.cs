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

using Algoloop.Wpf.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Logging;
using QuantConnect.Statistics;
using System;
using System.Collections.Generic;

namespace Algoloop.Wpf.Tests.ViewModel
{
    [TestClass()]
    public class BacktestViewModelTests
    {
        [TestInitialize()]
        public void Initialize()
        {
            Log.LogHandler = new ConsoleLogHandler();
        }

        [TestMethod()]
        public void CalculateScore_TradeViewModel_idealProfit()
        {
            var trades = new List<TradeViewModel>
            {
                new TradeViewModel{ ProfitLoss = 2, TotalFees = 1, EntryTime = new DateTime(2018,01,01), ExitTime = new DateTime(2018,02,01) },
                new TradeViewModel{ ProfitLoss = 2, TotalFees = 1, EntryTime = new DateTime(2018,03,01), ExitTime = new DateTime(2018,04,01) },
                new TradeViewModel{ ProfitLoss = 2, TotalFees = 1, EntryTime = new DateTime(2018,05,01), ExitTime = new DateTime(2018,06,01) },
                new TradeViewModel{ ProfitLoss = 2, TotalFees = 1, EntryTime = new DateTime(2018,07,01), ExitTime = new DateTime(2019,01,01) }
            };

            double score = BacktestViewModel.CalculateScore(trades);
            Assert.IsTrue(score == 1);
        }

        [TestMethod()]
        public void CalculateScore_TradeViewModel_idealLoss()
        {
            var trades = new List<TradeViewModel>
            {
                new TradeViewModel{ ProfitLoss = -2, TotalFees = 1, EntryTime = new DateTime(2018,01,01), ExitTime = new DateTime(2018,02,01) },
                new TradeViewModel{ ProfitLoss = -2, TotalFees = 1, EntryTime = new DateTime(2018,03,01), ExitTime = new DateTime(2018,04,01) },
                new TradeViewModel{ ProfitLoss = -2, TotalFees = 1, EntryTime = new DateTime(2018,05,01), ExitTime = new DateTime(2018,06,01) },
                new TradeViewModel{ ProfitLoss = -2, TotalFees = 1, EntryTime = new DateTime(2018,07,01), ExitTime = new DateTime(2019,01,01) }
            };

            double score = BacktestViewModel.CalculateScore(trades);
            Assert.IsTrue(score == -1);
        }

        [TestMethod()]
        public void CalculateScore_TradeViewModel_breakeven()
        {
            var trades = new List<TradeViewModel>
            {
                new TradeViewModel{ ProfitLoss = 5, EntryTime = new DateTime(2018,01,01), ExitTime = new DateTime(2018,02,01) },
                new TradeViewModel{ ProfitLoss = 3, EntryTime = new DateTime(2018,03,01), ExitTime = new DateTime(2018,04,01) },
                new TradeViewModel{ ProfitLoss = -5, EntryTime = new DateTime(2018,05,01), ExitTime = new DateTime(2018,06,01) },
                new TradeViewModel{ ProfitLoss = -3, EntryTime = new DateTime(2018,07,01), ExitTime = new DateTime(2019,01,01) }
            };

            double score = BacktestViewModel.CalculateScore(trades);
            Assert.IsTrue(score == 0);
        }

        [TestMethod()]
        public void CalculateScore_TradeViewModel_profit()
        {
            var trades = new List<TradeViewModel>
            {
                new TradeViewModel{ ProfitLoss = 5, MAE = -4, TotalFees = 1, EntryTime = new DateTime(2018,01,01), ExitTime = new DateTime(2018,02,01) },
                new TradeViewModel{ ProfitLoss = 3, TotalFees = 1, EntryTime = new DateTime(2018,03,01), ExitTime = new DateTime(2018,04,01) },
                new TradeViewModel{ ProfitLoss = 2, TotalFees = 1, EntryTime = new DateTime(2018,05,01), ExitTime = new DateTime(2018,06,01) },
                new TradeViewModel{ ProfitLoss = 2, TotalFees = 1, EntryTime = new DateTime(2018,07,01), ExitTime = new DateTime(2019,01,01) }
            };

            double score = BacktestViewModel.CalculateScore(trades);
            Assert.IsTrue(Math.Abs(score - 0.3120) < 0.0001);
        }

        [TestMethod()]
        public void CalculateScore_TradeViewModel_loss()
        {
            var trades = new List<TradeViewModel>
            {
                new TradeViewModel{ ProfitLoss = -5,  MAE = -4, EntryTime = new DateTime(2018,01,01), ExitTime = new DateTime(2018,02,01) },
                new TradeViewModel{ ProfitLoss = -3, EntryTime = new DateTime(2018,03,01), ExitTime = new DateTime(2018,04,01) },
                new TradeViewModel{ ProfitLoss = -2, EntryTime = new DateTime(2018,05,01), ExitTime = new DateTime(2018,06,01) },
                new TradeViewModel{ ProfitLoss = 2, EntryTime = new DateTime(2018,07,01), ExitTime = new DateTime(2019,01,01) }
            };

            double score = BacktestViewModel.CalculateScore(trades);
            Assert.IsTrue(Math.Abs(score + 0.2193) < 0.0001);
        }

        [TestMethod()]
        public void CalculateScore_Candlestick_idealProfit()
        {
            List<ISeriesPoint> trades = new()
            {
                new Candlestick{ Time = new DateTime(2018, 01, 01), Close = 10000},
                new Candlestick{ Time = new DateTime(2018, 03, 01), Close = 11000 },
                new Candlestick{ Time = new DateTime(2018, 05, 01), Close = 12000 },
                new Candlestick{ Time = new DateTime(2019, 01, 01), Close = 13000 },
            };

            double score = BacktestViewModel.CalculateScore(trades);
            Assert.IsTrue(score == 1);
        }

        [TestMethod()]
        public void CalculateScore_Candlestick_idealLoss()
        {
            List<ISeriesPoint> trades = new()
            {
                new Candlestick{ Time = new DateTime(2018, 01, 01), Close = 10000},
                new Candlestick{ Time = new DateTime(2018, 03, 01), Close = 9000},
                new Candlestick{ Time = new DateTime(2018, 05, 01), Close = 8000 },
                new Candlestick{ Time = new DateTime(2019, 01, 01), Close = 7000 },
            };

            double score = BacktestViewModel.CalculateScore(trades);
            Assert.IsTrue(score == -1);
        }

        [TestMethod()]
        public void CalculateScore_Candlestick_breakeven()
        {
            List<ISeriesPoint> trades = new()
            {
                new Candlestick{ Time = new DateTime(2018,01,01), Close = 10000 },
                new Candlestick{ Time = new DateTime(2018,03,01), Close = 9000 },
                new Candlestick{ Time = new DateTime(2018,05,01), Close = 8000 },
                new Candlestick{ Time = new DateTime(2019,01,01), Close = 10000 },
            };

            double score = BacktestViewModel.CalculateScore(trades);
            Assert.IsTrue(score == 0);
        }

        [TestMethod()]
        public void CalculateScore_Candlestick_profit()
        {
            List<ISeriesPoint> trades = new()
            {
                new Candlestick{ Time = new DateTime(2018, 01, 01), Close = 10000},
                new Candlestick{ Time = new DateTime(2018, 03, 01), Close = 11000 },
                new Candlestick{ Time = new DateTime(2018, 05, 01), Close = 13000 },
                new Candlestick{ Time = new DateTime(2019, 01, 01), Close = 16000 },
            };

            double score = BacktestViewModel.CalculateScore(trades);
            Assert.IsTrue(Math.Abs(score - 0.7698) < 0.0001);
        }
    }
}
