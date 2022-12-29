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

using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Benchmarks;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using QuantConnect.Statistics;

namespace Algoloop.Algorithm.CSharp.Model
{
    // Portfolio construction scaffolding class; basic method args.
    public class SlotPortfolio : PortfolioConstructionModel
    {
        private readonly bool _logTargets = false;
        private readonly bool _logTrades = false;
        private readonly bool _logInsights = false;

        private readonly int _slots;
        private readonly bool _reinvest;
        private readonly decimal _rebalance;
        private readonly int _trackerPeriod1;
        private readonly int _trackerPeriod2;
        private readonly int _indexPeriod1;
        private readonly int _indexPeriod2;
        private readonly decimal _stoplossSizing;
        private readonly InsightCollection _insights = new();
        private readonly RateOfChange _trackerRoc;
        private readonly RateOfChange _benchmarkRoc;
        private readonly RateOfChange _portfolioRoc;

        private TrackerPortfolio _trackerPortfolio;
        private SimpleMovingAverage _trackerSma1;
        private SimpleMovingAverage _trackerSma2;
        private SimpleMovingAverage _benchmarkSma1;
        private SimpleMovingAverage _benchmarkSma2;
        private decimal _initialCapital = 0;
        private decimal _portfolioValue0 = 0;
        private decimal _trackerValue0 = 0;
        private decimal _benchmarkValue0 = 0;
        private decimal _sizingFactor = 1;
        private decimal _leverage = 1;
        private string _indexName = "Index";

        public SlotPortfolio(
            int slots,
            bool reinvest,
            float rebalance,
            int trackerPeriod1 = 0,
            int trackerPeriod2 = 0,
            int indexPeriod1 = 0,
            int indexPeriod2 = 0,
            decimal stoplossSizing = 0)
        {
            _slots = slots;
            _reinvest = reinvest;
            _rebalance = (decimal)rebalance;
            _trackerPeriod1 = trackerPeriod1;
            _trackerPeriod2 = trackerPeriod2;
            _indexPeriod1 = indexPeriod1;
            _indexPeriod2 = indexPeriod2;
            _stoplossSizing = stoplossSizing;
            _trackerRoc = new RateOfChange(1);
            _benchmarkRoc = new RateOfChange(1);
            _portfolioRoc = new RateOfChange(1);
        }

        // Create list of PortfolioTarget objects from Insights
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            // Initialize ?
            if (_initialCapital == 0)
            {
                Initialize(algorithm);
            }

            // End of algorithm ?
            if (insights == null)
            {
                _trackerPortfolio.CreateTargets(algorithm, null);
                LogInsights(algorithm, _insights.OrderByDescending(m => m.Magnitude));
                if (_logTrades)
                {
                    LogTrades(algorithm);
                }

                return null;
            }

            // Remove obsolete insights
            _insights.RemoveExpiredInsights(algorithm.UtcTime);
            foreach (Insight insight in insights)
            {
                Insight obsolete = _insights.FirstOrDefault(m => m.Symbol.Equals(insight.Symbol));
                if (obsolete == null)
                    continue;
                _insights.Remove(obsolete);
            }

            // Merge insight lists and sort decending by magnitude
            _insights.AddRange(insights);
            insights = _insights.OrderByDescending(m => m.Magnitude).ToArray();
            _trackerPortfolio.CreateTargets(algorithm, insights);
            _insights.Clear();
            _insights.AddRange(insights); // Add in random order
            if (_logInsights)
            {
                LogInsights(algorithm, insights);
            }

            // Plot portfolio, tracker and benchmark index
            decimal trackerValue = _trackerPortfolio.GetEquity(algorithm);
            decimal benchmarkValue = algorithm.Benchmark.Evaluate(algorithm.Time);
            decimal portfolioValue = algorithm.Portfolio.TotalPortfolioValue;
            decimal trackerSizingFactor = ProcessTracker(algorithm, trackerValue);
            decimal indexSizingFactor = ProcessBenchmark(algorithm, benchmarkValue);
            ProcessPortfolio(algorithm, portfolioValue);
            PlotRoc(algorithm);

            // Determine leverage
            decimal sizingFactor = trackerSizingFactor * indexSizingFactor;
            if (sizingFactor != _sizingFactor)
            {
                _leverage = sizingFactor * portfolioValue / trackerValue;
                algorithm.Log($"Switching to scale {_leverage:0.000}");
                _sizingFactor = sizingFactor;
            }

            // Get targets
            var targets = _trackerPortfolio.GetTargets(_leverage);
            if (_logTargets)
            {
                LogTargets(algorithm, targets);
            }

            return targets;
        }

        private void Initialize(QCAlgorithm algorithm)
        {
            // Save initial capital for future use
            _initialCapital = algorithm.Portfolio.Cash;
            _trackerPortfolio = new TrackerPortfolio(_initialCapital, _slots, _reinvest, _rebalance);
            _trackerValue0 = _trackerPortfolio.GetEquity(algorithm);
            _portfolioValue0 = algorithm.Portfolio.TotalPortfolioValue;
            _benchmarkValue0 = algorithm.Benchmark.Evaluate(algorithm.Time);
            if (algorithm.Benchmark is SecurityBenchmark sec)
            {
                _indexName = sec.Security.Symbol.ID.Symbol;
            }
            if (_trackerPeriod1 > 0)
            {
                _trackerSma1 = new SimpleMovingAverage($"Index Tracker SMA({_trackerPeriod1})", _trackerPeriod1);
            }
            if (_trackerPeriod2 > 0)
            {
                _trackerSma2 = new SimpleMovingAverage($"Index Tracker SMA({_trackerPeriod2})", _trackerPeriod2);
            }
            if (_indexPeriod1 > 0)
            {
                _benchmarkSma1 = new SimpleMovingAverage($"Index {_indexName} SMA({_indexPeriod1})", _indexPeriod1);
            }
            if (_indexPeriod2 > 0)
            {
                _benchmarkSma2 = new SimpleMovingAverage($"Index {_indexName} SMA({_indexPeriod2})", _indexPeriod2);
            }
        }

        private void ProcessPortfolio(QCAlgorithm algorithm, decimal portfolioValue)
        {
            decimal portfolioIndex = 100 * portfolioValue / _portfolioValue0;
            algorithm.Plot("Index Portfolio", portfolioIndex);
            _portfolioRoc.Update(algorithm.Time, portfolioIndex);
        }

        private decimal ProcessTracker(QCAlgorithm algorithm, decimal trackerValue)
        {
            decimal trackerIndex = 100 * trackerValue / _trackerValue0;
            algorithm.Plot("Index Tracker", trackerIndex);
            _trackerRoc.Update(algorithm.Time, trackerIndex);

            if (_trackerSma1 != null)
            {
                _trackerSma1.Update(algorithm.Time, trackerIndex);
                if (_trackerSma1.IsReady)
                {
                    algorithm.Plot($"TrackerSMA", _trackerSma1);
                }
            }
            if (_trackerSma2 != null)
            {
                _trackerSma2.Update(algorithm.Time, trackerIndex);
                if (_trackerSma2.IsReady)
                {
                    algorithm.Plot($"TrackerSMA", _trackerSma2);
                }
            }

            decimal sizingFactor = 1;
            if (_trackerSma1 != null && _trackerSma2 != null)
            {
                if (_trackerSma1.IsReady && _trackerSma2.IsReady)
                {
                    sizingFactor = _trackerSma1 >= _trackerSma2 ? 1 : _stoplossSizing;
                }
            }
            else if (_trackerSma1 != null)
            {
                if (_trackerSma1.IsReady)
                {
                    sizingFactor = trackerIndex >= _trackerSma1 ? 1 : _stoplossSizing;
                }
            }
            else if (_trackerSma2 != null)
            {
                if (_trackerSma2.IsReady)
                {
                    sizingFactor = trackerIndex >= _trackerSma2 ? 1 : _stoplossSizing;
                }
            }

            return sizingFactor;
        }

        private decimal ProcessBenchmark(QCAlgorithm algorithm, decimal benchmarkValue)
        {
            // Plot benchmark index
            decimal benchmarkIndex = 100 * benchmarkValue / _benchmarkValue0;
            algorithm.Plot($"Index {_indexName}", benchmarkIndex);
            _benchmarkRoc.Update(algorithm.Time, benchmarkValue);

            if (_benchmarkSma1 != null)
            {
                _benchmarkSma1.Update(algorithm.Time, benchmarkIndex);
                if (_benchmarkSma1.IsReady)
                {
                    algorithm.Plot($"IndexSMA", _benchmarkSma1);
                }
            }
            if (_benchmarkSma2 != null)
            {
                _benchmarkSma2.Update(algorithm.Time, benchmarkIndex);
                if (_benchmarkSma2.IsReady)
                {
                    algorithm.Plot($"IndexSMA", _benchmarkSma2);
                }
            }

            decimal sizingFactor = 1;
            if (_benchmarkSma1 != null && _benchmarkSma2 != null)
            {
                if (_benchmarkSma1.IsReady && _benchmarkSma2.IsReady)
                {
                    sizingFactor = _benchmarkSma1 >= _benchmarkSma2 ? 1 : _stoplossSizing;
                }
            }
            else if (_benchmarkSma1 != null)
            {
                if (_benchmarkSma1.IsReady)
                {
                    sizingFactor = benchmarkIndex >= _benchmarkSma1 ? 1 : _stoplossSizing;
                }
            }
            else if (_benchmarkSma2 != null)
            {
                if (_benchmarkSma2.IsReady)
                {
                    sizingFactor = benchmarkIndex >= _benchmarkSma2 ? 1 : _stoplossSizing;
                }
            }

            return sizingFactor;
        }

        private void PlotRoc(QCAlgorithm algorithm)
        {
            // Compare portfolio vs index
            if (_portfolioRoc.IsReady && _benchmarkRoc.IsReady)
            {
                decimal diff = _trackerRoc - _benchmarkRoc;
                algorithm.Plot($"Portfolio vs {_indexName}", diff);
            }
        }

        private void LogTrades(QCAlgorithm algorithm)
        {
            decimal cash = _initialCapital;
            foreach (Trade trade in algorithm.TradeBuilder.ClosedTrades)
            {
                decimal profit = trade.Quantity * (trade.ExitPrice - trade.EntryPrice);
                cash += profit;
                algorithm.Log($"Trade {trade.EntryTime.ToShortDateString()} {trade.ExitTime.ToShortDateString()} {trade.Symbol} Size={trade.Quantity:0.00} Entry={trade.EntryPrice:0.00} Exit={trade.ExitPrice:0.00} Profit={profit:0.00} Cash={cash:0.00}");
            }

            foreach (SecurityHolding holding in algorithm.Portfolio.Values)
            {
                if (!holding.Invested) continue;
                decimal profit = holding.Quantity * (holding.Price - holding.AveragePrice);
                cash += profit;
                algorithm.Log($"Trade {algorithm.Time.ToShortDateString()} {holding.Symbol.ID.Symbol} Size={holding.Quantity:0.00} Entry={holding.AveragePrice:0.00} Exit={holding.Price:0.00} Profit={profit:0.00} Cash={cash:0.00}");
            }
        }

        private static void LogInsights(QCAlgorithm algorithm, IEnumerable<Insight> insights)
        {
            int i = 0;
            algorithm.Log("Insight toplist:");
            foreach (Insight insight in insights)
            {
                string expired = insight.IsExpired(algorithm.UtcTime) ? "Expired" : String.Empty;
                algorithm.Log($"Insight {++i} {insight.Symbol} {insight.Magnitude:0.########} {insight.Direction} {expired}".ToStringInvariant());
            }
        }

        private static void LogTargets(QCAlgorithm algorithm, IEnumerable<IPortfolioTarget> targets)
        {
            int i = 0;
            foreach (IPortfolioTarget target in targets)
            {
                algorithm.Log($"Target {++i} {target.Symbol} quantity={target.Quantity:0.##}".ToStringInvariant());
            }
        }
    }
}
