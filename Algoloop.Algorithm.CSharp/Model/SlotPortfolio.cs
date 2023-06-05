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
        private const decimal TradeSizing = 1m;
        private const decimal StoplossSizing = 0m;
        private const string TrackerChart = "Tracker";

        private readonly bool _logTargets = false;
        private readonly bool _logTrades = false;
        private readonly bool _logInsights = false;

        private readonly int _slots;
        private readonly bool _reinvest;
        private readonly decimal _rebalance;
        private readonly InsightCollection _insights = new();
        private readonly SimpleMovingAverage _trackerSma1;
        private readonly SimpleMovingAverage _trackerSma2;
        private TrackerPortfolio _trackerPortfolio;
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
            float rebalance = 0,
            int trackerPeriod1 = 0,
            int trackerPeriod2 = 0)
        {
            _slots = slots;
            _reinvest = reinvest;
            _rebalance = (decimal)rebalance;
            if (trackerPeriod1 > 0)
            {
                _trackerSma1 = new SimpleMovingAverage($"Tracker SMA({trackerPeriod1})", trackerPeriod1);
            }
            if (trackerPeriod2 > 0 && trackerPeriod2 > trackerPeriod1)
            {
                _trackerSma2 = new SimpleMovingAverage($"Tracker SMA({trackerPeriod2})", trackerPeriod2);
            }
        }

        // Create list of PortfolioTarget objects from Insights
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            if (algorithm.IsWarmingUp) return Enumerable.Empty<IPortfolioTarget>();

            // Initialize ?
            if (_initialCapital == 0)
            {
                Initialize(algorithm);
            }

            // End of algorithm ?
            if (insights == null)
            {
                _trackerPortfolio.CreateTargets(algorithm, null);
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
            _trackerPortfolio.CreateTargets(algorithm, insights);
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
            algorithm.Plot(TrackerChart, "Tracker Portfolio", portfolioIndex);

            decimal trackerValue = _trackerPortfolio.GetEquity(algorithm);
            decimal trackerIndex = trackerValue / _trackerValue0;
            algorithm.Plot(TrackerChart, "Tracker", trackerIndex);

            decimal trackerSma2 = _trackerSma2 ?? 0m;
            _trackerSma1?.Update(algorithm.Time, trackerIndex);
            _trackerSma2?.Update(algorithm.Time, trackerIndex);

            decimal sizingFactor = TradeSizing;
            if (_trackerSma1 != null && _trackerSma1.IsReady)
            {
                algorithm.Plot(TrackerChart, $"Tracker SMA({_trackerSma1.Period})", _trackerSma1);
                sizingFactor = trackerIndex >= _trackerSma1 ? TradeSizing : StoplossSizing;
            }
            if (_trackerSma2 != null && _trackerSma2.IsReady)
            {
                algorithm.Plot(TrackerChart, $"Tracker SMA({_trackerSma2.Period})", _trackerSma2);
                sizingFactor = _trackerSma2 >= trackerSma2 ? TradeSizing : StoplossSizing;
            }
            if (_trackerSma1 != null && _trackerSma2 != null)
            {
                if (_trackerSma1.IsReady && _trackerSma2.IsReady)
                {
                    sizingFactor = _trackerSma1 >= _trackerSma2 ? TradeSizing : StoplossSizing;
                }
                else
                {
                    sizingFactor = TradeSizing;
                }
            }

            // Plot tracker and benchmark index
            decimal benchmarkValue = algorithm.Benchmark.Evaluate(algorithm.Time);
            decimal benchmarkIndex = benchmarkValue / _benchmarkValue0;
            if (benchmarkIndex > 0)
            {
                algorithm.Plot(TrackerChart, $"Tracker {_indexName}", benchmarkIndex);
                algorithm.Plot(TrackerChart, $"Tracker / {_indexName}", trackerIndex / benchmarkIndex);
            }

            // Determine leverage
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
