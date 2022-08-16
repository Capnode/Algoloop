/*
 * Copyright 2022 Capnode AB
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
using QuantConnect.Securities;

namespace Algoloop.Algorithm.CSharp.Model
{
    internal class TrackerPortfolio : PortfolioConstructionModel
    {
        private readonly int _slots;
        private readonly decimal _rebalance;
        private readonly decimal _size;
        private decimal _cash = 100;
        private IDictionary<Symbol, decimal> _positions = new Dictionary<Symbol, decimal>();

        public TrackerPortfolio(int slots, decimal rebalance)
        {
            _slots = slots;
            _rebalance = rebalance;
            _size = _cash / slots;
        }

        // Create list of PortfolioTarget objects from Insights
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            IEnumerable<Insight> toplist = insights.Take(_slots);
//            LogInsights(algorithm, toplist);

            // Exit position if not i toplist
            foreach (KeyValuePair<Symbol, decimal> position in _positions.ToArray())
            {
                if (toplist.Any(m => m.Symbol.Equals(position.Key))) continue;
                Security security = algorithm.Securities[position.Key];
                decimal value = security.Price * position.Value;
                _cash += value;
                if (!_positions.Remove(position.Key)) throw new ApplicationException($"Can not remove {position.Key}");
            }

            // Add position if new in toplist
            int freeSlots = _slots - _positions.Count;
            decimal size = freeSlots > 0 ? Math.Min(_size, _cash / freeSlots) : 0;
            foreach (Insight insight in toplist)
            {
                Security security = algorithm.Securities[insight.Symbol];
                decimal target = _size / security.Price;
                if (_positions.TryGetValue(insight.Symbol, out decimal holdings))
                {
                    if (_rebalance > 0 &&
                        (holdings <= (1 - _rebalance) * target || holdings >= (1 + _rebalance) * target))
                    {
                        _cash -= security.Price * (target - holdings);
                        _positions[insight.Symbol] = target;
                    }
                }
                else // New position
                {
                    _cash -= size;
                    _positions.Add(insight.Symbol, target);
                }
            }

            if (_positions.Count > _slots) throw new ApplicationException("Too many positions");
            if (_size < 0) throw new ApplicationException("Negative balance");
            return null;
        }

        public decimal GetEquity(QCAlgorithm algorithm)
        {
            decimal equity = _cash;
            foreach (KeyValuePair<Symbol, decimal> position in _positions)
            {
                Security security = algorithm.Securities[position.Key];
                decimal value = security.Price * position.Value;
                equity += value;
            }

            return equity.SmartRounding();
        }
    }
}
