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
            if (_logTargets)
            {
                LogTargets(algorithm, targets);
            }

            // Calculate number of occupied positions
            int taken = algorithm.Securities.Values
                .Where(m => m.HasData && m.Holdings.Quantity != 0)
                .Count();
            Debug.Assert(taken <= _slots);

            // Cancel all pending orders
            algorithm.Transactions.CancelOpenOrders();

            // Place new orders
            foreach (IPortfolioTarget target in targets)
            {
                // Get holdings for symbol
                Security security = algorithm.Securities[target.Symbol];
                decimal holdings = security.Holdings.Quantity;
                decimal quantity = target.Quantity - holdings;
                if (quantity == 0)
                    continue;

                if (quantity < 0 || holdings != 0)
                {
                    algorithm.LimitOrder(target.Symbol, quantity, security.Close);
                    if (_logOrder)
                    {
                        algorithm.Log($"Limit {target.Symbol} quantity={quantity:0} price={security.Close:0.####}".ToStringInvariant());
                    }
                }
                else if (taken < _slots)
                {
                    taken++;
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

        private static void LogTargets(QCAlgorithm algorithm, IEnumerable<IPortfolioTarget> targets)
        {
            int i = 0;
            foreach (IPortfolioTarget target in targets)
            {
                algorithm.Log($"Pending {++i} {target.Symbol} quantity={target.Quantity:0.##}".ToStringInvariant());
            }
        }
    }
}
