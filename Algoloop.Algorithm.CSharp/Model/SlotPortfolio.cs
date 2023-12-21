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
        private const string TrackerChart = "Tracker";

        private readonly bool _logTargets = false;
        private readonly bool _logTrades = false;
        private readonly bool _logInsights = false;

        private readonly int _slots;
        private readonly bool _reinvest;
        private readonly decimal _rebalance;
        private readonly decimal _reduction;
        private readonly InsightCollection _insights = new();
        private readonly SimpleMovingAverage _sma;

        private Tracker _tracker;
        private decimal _initialCapital = 0;
        private decimal _portfolioValue0 = 0;
        private decimal _trackerValue0 = 0;
        private decimal _benchmarkValue0 = 0;
        private string _indexName = "Index";

        public SlotPortfolio(
            int slots,
            bool reinvest,
            float rebalance = 0,
            int smaPeriod = 0,
            float reduction = 0)
        {
            _slots = slots;
            _reinvest = reinvest;
            _rebalance = (decimal)rebalance;
            _reduction = (decimal)reduction;
            if (smaPeriod > 0)
            {
                _sma = new SimpleMovingAverage($"SMA({smaPeriod})", smaPeriod);
            }
        }

        // Create list of PortfolioTarget objects from Insights
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            var time = algorithm.Time;
            if (algorithm.IsWarmingUp) return Enumerable.Empty<IPortfolioTarget>();

            // Initialize ?
            if (_initialCapital == 0)
            {
                Initialize(algorithm);
            }

            // End of algorithm ?
            if (insights == null)
            {
                _tracker.CreateTargets(algorithm, null);
                LogInsights(algorithm, _insights);
                if (_logTrades)
                {
                    LogTrades(algorithm);
                }

                return null;
            }

            // Remove expired insights
            _insights.RemoveExpiredInsights(algorithm.UtcTime);

            // Add new insights, do not replace existing
            foreach (Insight insight in insights)
            {
                if (!_insights.Any(m => m.Symbol.Equals(insight.Symbol)))
                {
                    _insights.Add(insight);
                }
            }

            // Sort insights decending by magnitude
            insights = _insights
                .OrderByDescending(m => m.Magnitude)
                .ThenBy(m => m.Symbol.ID.Symbol)
                .ToArray();
            IEnumerable<IPortfolioTarget> targets = _tracker.CreateTargets(algorithm, insights);
            Symbol[] symbols = _insights.Select(m => m.Symbol).ToArray();
            _insights.Clear(symbols);
            _insights.AddRange(insights); // Add in random order
            if (_logInsights)
            {
                LogInsights(algorithm, insights);
            }

            // Plot portfolio
            decimal portfolioValue = algorithm.Portfolio.TotalPortfolioValue;
            decimal portfolioIndex = portfolioValue / _portfolioValue0;
            algorithm.Plot(TrackerChart, "Portfolio", portfolioIndex);

            decimal trackerValue = _tracker.GetEquity(algorithm);
            decimal trackerIndex = trackerValue / _trackerValue0;
            algorithm.Plot(TrackerChart, "Tracker", trackerIndex);

            // Plot tracker and benchmark index
            decimal benchmarkValue = algorithm.Benchmark.Evaluate(algorithm.Time);
            if (benchmarkValue > 0 && _benchmarkValue0 > 0)
            {
                decimal benchmarkIndex = benchmarkValue / _benchmarkValue0;
                decimal portfolioBenchmarkIndex = portfolioIndex / benchmarkIndex;
                decimal trackerBenchmarkIndex = trackerIndex / benchmarkIndex;
                algorithm.Plot(TrackerChart, $"Benchmark {_indexName}", benchmarkIndex);
                algorithm.Plot(TrackerChart, $"Portfolio / {_indexName}", portfolioBenchmarkIndex);
                algorithm.Plot(TrackerChart, $"Tracker / {_indexName}", trackerBenchmarkIndex);

                // SMA stoploss on Tracker / Benchmark
                _sma?.Update(algorithm.Time, trackerBenchmarkIndex);
                if (_sma != null && _sma.IsReady)
                {
                    algorithm.Plot(TrackerChart, $"Tracker  / {_indexName} SMA({_sma.Period})", _sma);
                    decimal maxScale = portfolioValue / trackerValue;
                    decimal scale = trackerBenchmarkIndex < _sma ? _reduction * maxScale : maxScale;
                    algorithm.Plot(TrackerChart, $"Portfolio Scale", scale);
                    _tracker.Scale = scale;
                }
            }
            else
            {
                // SMA stoploss on Tracker
                _sma?.Update(algorithm.Time, trackerIndex);
                if (_sma != null && _sma.IsReady)
                {
                    algorithm.Plot(TrackerChart, $"Tracker SMA({_sma.Period})", _sma);
                    decimal maxScale = portfolioValue / trackerValue;
                    decimal scale = trackerIndex < _sma ? _reduction * maxScale : maxScale;
                    algorithm.Plot(TrackerChart, $"Portfolio Scale", scale);
                    _tracker.Scale = scale;
                }
            }

            if (_logTargets)
            {
                LogTargets(algorithm, targets);
            }

            algorithm.Plot(TrackerChart, "Portfolio targets", targets.Count(m => m.Quantity != 0));
            return targets;
        }

        private void Initialize(QCAlgorithm algorithm)
        {
            // Save initial capital for future use
            _initialCapital = algorithm.Portfolio.Cash;
            _tracker = new Tracker(_initialCapital, _slots, _reinvest, _rebalance);
            _trackerValue0 = _tracker.GetEquity(algorithm);
            _portfolioValue0 = algorithm.Portfolio.TotalPortfolioValue;
            _benchmarkValue0 = algorithm.Benchmark.Evaluate(algorithm.Time);
            if (algorithm.Benchmark is SecurityBenchmark sec)
            {
                _indexName = sec.Security.Symbol.ID.Symbol;
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
            foreach (Insight insight in insights.OrderByDescending(m => m.Magnitude))
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
