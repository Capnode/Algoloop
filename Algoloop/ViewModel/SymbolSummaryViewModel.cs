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
using QuantConnect.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Algoloop.ViewModel
{
    public class SymbolSummaryViewModel
    {
        List<Trade> _trades = new List<Trade>();
        private readonly Symbol _symbol;

        public SymbolSummaryViewModel(Symbol symbol)
        {
            _symbol = symbol;
        }

        /// <summary>
        /// The symbol of the traded instrument
        /// </summary>
        public string Symbol => _symbol.Value;

        /// <summary>
        /// The type of the traded instrument
        /// </summary>
        public string Type => _symbol.SecurityType.ToString();

        /// <summary>
        /// The number of trades
        /// </summary>
        public int Trades => _trades.Count;

        /// <summary>
        /// The gross profit/loss of the trades (as account currency)
        /// </summary>
        public decimal ProfitLoss { get; private set; }

        /// <summary>
        /// The maximum drawdown of the trades (as account currency)
        /// </summary>
        public decimal Drawdown { get; private set; }

        /// <summary>
        /// Total profit to max drawdown ratio
        /// </summary>
        public decimal DDRatio { get; private set; }

        public void AddTrade(Trade trade)
        {
            _trades.Add(trade);
        }

        public void Calculate()
        {
            decimal profitLoss = _trades.Sum(m => m.ProfitLoss);
            ProfitLoss = Math.Round(profitLoss, 2);
            IEnumerable<decimal> range = _trades.Select(m => m.MFE - m.MAE);
            decimal stddev = StandardDeviation(range);
            decimal sharpe = stddev == 0 ? 0 : ProfitLoss / stddev;
            decimal drawdown = MaxDrawdown();
            Drawdown = Math.Round(drawdown, 2);
            decimal ratio = drawdown == 0 ? 0 : ProfitLoss / -drawdown;
            DDRatio = Math.Round(ratio, 2);
        }

        private decimal MaxDrawdown()
        {
            decimal top = 0;
            decimal bottom = 0;
            decimal close = 0;
            decimal drawdown = 0;
            foreach(var trade in _trades)
            {
                if (close + trade.MFE > top)
                {
                    top = close + trade.MFE;
                    bottom = close + trade.ProfitLoss;
                }
                else
                {
                    bottom = Math.Min(bottom, close + trade.MAE);
                }

                drawdown = Math.Min(drawdown, bottom - top);
                close = close + trade.ProfitLoss;
            }

            return drawdown;
        }

        private static decimal StandardDeviation(IEnumerable<decimal> values)
        {
            int count = values.Count();
            if (count == 1)
                return 0;

            //Compute the Average
            decimal avg = values.Average();

            //Perform the Sum of (value-avg)^2
            decimal sum = values.Sum(d => (d - avg) * (d - avg));

            //Put it all together
            return (decimal)Math.Sqrt((double)(sum / count));
        }
    }
}