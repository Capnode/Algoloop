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
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Statistics;
using System.Diagnostics;

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
        private readonly int _trackerPeriod;
        private readonly int _indexPeriod;
        private readonly InsightCollection _insights = new();
        private readonly RateOfChange _trackerRoc;
        private readonly RateOfChange _benchmarkRoc;
        private readonly RateOfChange _portfolioRoc;
        private TrackerPortfolio _trackerPortfolio;
        private SimpleMovingAverage _trackerSma;
        private SimpleMovingAverage _indexSma;
        private decimal _initialCapital = 0;
        private decimal _portfolioValue0 = 0;
        private decimal _trackerValue0 = 0;
        private decimal _benchmarkValue0 = 0;
        private decimal _sizingFactor = 1;
        private string _indexName = "Index";

        public SlotPortfolio(
            int slots,
            bool reinvest,
            float rebalance,
            int trackerPeriod = 0,
            int indexPeriod = 0)
        {
            _slots = slots;
            _reinvest = reinvest;
            _rebalance = (decimal)rebalance;
            _trackerPeriod = trackerPeriod;
            _indexPeriod = indexPeriod;
            _trackerRoc = new RateOfChange(1);
            _benchmarkRoc = new RateOfChange(1);
            _portfolioRoc = new RateOfChange(1);
        }

        // Create list of PortfolioTarget objects from Insights
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            if (insights == null)
            {
                // End of algorithm
                _trackerPortfolio.CreateTargets(algorithm, null);
                LogInsights(algorithm, _insights.OrderByDescending(m => m.CloseTimeUtc).ThenByDescending(m => m.Magnitude));
                if (_logTrades)
                {
                    LogTrades(algorithm);
                }

                return null;
            }

            // Initialize
            if (_initialCapital == 0)
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
                if (_trackerPeriod > 0)
                {
                    _trackerSma = new SimpleMovingAverage($"Index Tracker SMA({_trackerPeriod})", _trackerPeriod);
                }
                if (_indexPeriod > 0)
                {
                    _indexSma = new SimpleMovingAverage($"Index {_indexName} SMA({_indexPeriod})", _indexPeriod);
                }
            }

            // Plot benchmark index
            decimal benchmarkValue = algorithm.Benchmark.Evaluate(algorithm.Time);
            decimal benchmarkIndex = 100 * benchmarkValue / _benchmarkValue0;
            algorithm.Plot($"Index {_indexName}", benchmarkIndex);
            _benchmarkRoc.Update(algorithm.Time, benchmarkValue);

            // Plot tracker index
            _trackerPortfolio.CreateTargets(algorithm, insights);
            decimal trackerValue = _trackerPortfolio.GetEquity(algorithm);
            decimal trackerIndex = 100 * trackerValue / _trackerValue0;
            algorithm.Plot("Index Tracker", trackerIndex);
            _trackerRoc.Update(algorithm.Time, trackerIndex);

            // Plot portfolio index
            decimal portfolioValue = algorithm.Portfolio.TotalPortfolioValue;
            decimal portfolioIndex = 100 * portfolioValue / _portfolioValue0;
            algorithm.Plot("Index Portfolio", portfolioIndex);
            _portfolioRoc.Update(algorithm.Time, trackerIndex);

            // Compare portfolio vs index
            if (_portfolioRoc.IsReady && _benchmarkRoc.IsReady)
            {
                decimal diff = _trackerRoc - _benchmarkRoc;
                algorithm.Plot($"Portfolio vs {_indexName}", diff);
            }

            // Determine position size dependent on tracking curve
            decimal trackerSizingFactor = 1;
            if (_trackerSma != null)
            {
                _trackerSma.Update(algorithm.Time, trackerIndex);
                if (_trackerSma.IsReady)
                {
                    algorithm.Plot($"TrackerSMA", _trackerSma);
                    trackerSizingFactor = trackerIndex >= _trackerSma ? 1 : 0;
                }
            }

            // Determine position size dependent on index curve
            decimal indexSizingFactor = 1;
            if (_indexSma != null)
            {
                _indexSma.Update(algorithm.Time, benchmarkIndex);
                if (_indexSma.IsReady)
                {
                    algorithm.Plot($"IndexSMA", _indexSma);
                    indexSizingFactor = benchmarkIndex >= _indexSma ? 1 : 0;
                }
            }

            decimal sizingFactor = trackerSizingFactor * indexSizingFactor;
            if (sizingFactor != _sizingFactor)
            {
                algorithm.Log($"Switching to sizing factor {sizingFactor}");
            }
            _sizingFactor = sizingFactor;

            // Calculate number of occupied positions
            int taken = algorithm.Securities.Values
                .Where(m => m.HasData && m.Holdings.Quantity != 0)
                .Count();
            int freeSlots = _slots - taken;
            decimal cash = algorithm.Portfolio.Cash;

            // Create targets for all insights in toplist
            var targets = new List<IPortfolioTarget>();
            IEnumerable<Insight> toplist = insights.Take(_slots);
            foreach (Insight insight in toplist)
            {
                // Find current holdings and actual target
                Security security = algorithm.Securities[insight.Symbol];
                decimal holdings = security.Holdings.Quantity;
                IPortfolioTarget target;
                if (_reinvest)
                {
                    target = PortfolioTarget.Percent(algorithm, insight.Symbol, (int)insight.Direction * _sizingFactor / _slots);
                    if (holdings == 0 && freeSlots > 0)
                    {
                        decimal amount = Math.Max(_sizingFactor * cash, 0) / freeSlots;
                        decimal size = (int)insight.Direction * decimal.Floor(amount / security.Price);
                        var order = new MarketOrder(insight.Symbol, size, DateTime.UtcNow);
                        OrderFee orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, order));
                        amount -= decimal.Ceiling(orderFee);
                        amount = Math.Max(amount, 0);
                        decimal quantity = (int)insight.Direction * decimal.Floor(amount / security.Price);
                        if (quantity < target.Quantity)
                        {
                            target = new PortfolioTarget(insight.Symbol, quantity);
                        }
                    }
                }
                else
                {
                    decimal amount = _initialCapital / _slots;
                    decimal quantity = (int)insight.Direction * decimal.Floor(_sizingFactor * amount / security.Price);
                    target = new PortfolioTarget(insight.Symbol, quantity);
                }

                if (holdings == 0 || target.Quantity == 0)
                {
                    // Create new target
                    targets.Add(target);
                }
                else if (_rebalance > 0 && holdings <= decimal.Round((1 - _rebalance) * target.Quantity, 6))
                {
                    // Holdings too small, rebalance up
                    algorithm.Log($"Rebalance up {target.Symbol} {holdings} to {target.Quantity} @ {security.Price:0.00}");
                    targets.Add(target);
                }
                else if (_rebalance > 0 && holdings >= decimal.Round((1 + _rebalance) * target.Quantity, 6))
                {
                    // Holdings too large, rebalance down
                    algorithm.Log($"Rebalance down {target.Symbol} {holdings} to {target.Quantity} @ {security.Price:0.00}");
                    targets.Add(target);
                }
                else
                {
                    // Remove current insight, add again later
                    _insights.Clear(new[] { insight.Symbol });
                }
            }

            // Close expired insights
            foreach (Insight expired in _insights.RemoveExpiredInsights(algorithm.UtcTime))
            {
                // Skip if symbol in target list
                if (targets.Any(m => m.Symbol.Equals(expired.Symbol))) continue;

                // Check if expired holding still exists
                Security security = algorithm.Securities[expired.Symbol];
                decimal holdings = security.Holdings.Quantity;
                if (holdings != 0)
                {
                    // Create zero target
                    var target = new PortfolioTarget(expired.Symbol, 0);
                    targets.Add(target);

                    // Put it back into expired list
                    _insights.Add(expired);
                }
            }

            // Add toplist in random order
            _insights.AddRange(toplist);
            if (_logInsights)
            {
                LogInsights(algorithm, _insights.OrderByDescending(m => m.CloseTimeUtc).ThenByDescending(m => m.Magnitude));
            }

            if (_logTargets)
            {
                LogTargets(algorithm, targets);
            }

            return targets;
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

        private static void LogTargets(QCAlgorithm algorithm, List<IPortfolioTarget> targets)
        {
            int i = 0;
            foreach (IPortfolioTarget target in targets)
            {
                algorithm.Log($"Target {++i} {target.Symbol} quantity={target.Quantity:0.##}".ToStringInvariant());
            }
        }
    }

}
