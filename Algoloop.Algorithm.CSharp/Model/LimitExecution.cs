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
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using System.Diagnostics;

namespace Algoloop.Algorithm.CSharp.Model
{
    public class LimitExecution : ExecutionModel
    {
        private readonly bool _logTargets = false;
        private readonly bool _logOrder = false;

        private readonly int _slots;
        private readonly List<IPortfolioTarget> _pending = new List<IPortfolioTarget>();

        public LimitExecution(int slots)
        {
            _slots = slots;
        }

        /// <summary>
        /// Immediately submits orders for the specified portfolio targets.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The portfolio targets to be ordered</param>
        public override void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            if (algorithm == default) throw new ArgumentNullException(nameof(algorithm));

            // Remove existing duplicate targets
            List<IPortfolioTarget> duplicates = _pending
                .Where(m => targets.Any(p => p.Symbol.Equals(m.Symbol)))
                .ToList();
            duplicates.ForEach(m => _pending.Remove(m));

            // Add new targets to pending list
            _pending.AddRange(targets);

            // Make sure symbol only occurs once
            Debug.Assert(!_pending.GroupBy(x => x.Symbol).Any(g => g.Count() > 1));
            if (_logTargets)
            {
                LogTargets(algorithm, _pending);
            }

            // Calculate number of occupied positions
            int taken = algorithm.Securities.Values
                .Where(m => m.HasData && m.Holdings.Quantity != 0)
                .Count();
            Debug.Assert(taken <= _slots);

            foreach (IPortfolioTarget target in _pending.ToList())
            {
                // Cancel all pending orders for symbol
                algorithm.Transactions.CancelOpenOrders(target.Symbol);

                // Get holdings for symbol
                Security security = algorithm.Securities[target.Symbol];
                decimal holdings = security.Holdings.Quantity;
                decimal quantity = target.Quantity - holdings;

                // Check to see if we're done with this target
                if (Math.Abs(quantity) < security.SymbolProperties.LotSize)
                {
                    _pending.Remove(target);
                    continue;
                }

                if (target.Quantity == 0
                    || taken++ < _slots)
                {
                    algorithm.LimitOrder(target.Symbol, quantity, security.Close);
                    if (_logOrder)
                    {
                        algorithm.Log($"Limit {target.Symbol} quantity={quantity:0} price={security.Close:0.####}".ToStringInvariant());
                    }
                }
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
        }

        private void LogTargets(QCAlgorithm algorithm, List<IPortfolioTarget> targets)
        {
            int i = 0;
            foreach (IPortfolioTarget target in targets)
            {
                algorithm.Log($"Pending {++i} {target.Symbol} quantity={target.Quantity:0.##}".ToStringInvariant());
            }
        }
    }
}
