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
using System.Diagnostics;
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

        public decimal Score { get; private set; }

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
        /// Longest drawdown period
        /// </summary>
        public TimeSpan DrawdownPeriod { get; private set; }

        public decimal Expectancy { get; private set; }

        /// <summary>
        /// Total profit to max drawdown ratio
        /// </summary>
        public decimal RoMaD { get; private set; }

        public decimal Sortino { get; private set; }

        public void AddTrade(Trade trade)
        {
            _trades.Add(trade);
        }

        public void Calculate()
        {
            if (_trades == null || !_trades.Any())
            {
                return;
            }

            decimal profitLoss = _trades.Sum(m => m.ProfitLoss);
            ProfitLoss = Math.Round(profitLoss, 2);

            // Calculate Sortino ratio
            IEnumerable<decimal> range = _trades
                .Where(p => p.ProfitLoss < 0)
                .Select(m => m.ProfitLoss);
            decimal stddev = StandardDeviation(range);
            decimal sortino = stddev == 0 ? 0 : profitLoss / stddev;
            Sortino = sortino.RoundToSignificantDigits(4);

            // Calculate Return over Max Drawdown Ratio
            MaxDrawdown(out decimal drawdown, out TimeSpan period);
            Drawdown = Math.Round(drawdown, 2);
            DrawdownPeriod = period;
            decimal roMaD = drawdown == 0 ? 0 : profitLoss / -drawdown;
            RoMaD = roMaD.RoundToSignificantDigits(4);

            // Calculate score
            double score = TrackViewModel.CalculateScore(_trades, out double expectancy);
            score = score.RoundToSignificantDigits(4);
            expectancy = expectancy.RoundToSignificantDigits(4);
            Score = (decimal)score;
            Expectancy = (decimal)expectancy;
        }

        private void MaxDrawdown(out decimal drawdown, out TimeSpan period)
        {
            drawdown = 0;
            period = TimeSpan.Zero;
            if (!_trades.Any())
            {
                return;
            }

            decimal top = 0;
            decimal bottom = 0;
            decimal close = 0;
            DateTime topTime = _trades.First().EntryTime;
            foreach (var trade in _trades)
            {
                if (close + trade.MFE > top)
                {
                    top = close + trade.MFE;
                    bottom = close + trade.ProfitLoss;
                    topTime = trade.ExitTime;
                }
                else
                {
                    bottom = Math.Min(bottom, close + trade.MAE);
                    TimeSpan span = trade.ExitTime - topTime;
                    if (span > period)
                    {
                        period = span;
                    }
                }

                drawdown = Math.Min(drawdown, bottom - top);
                close += trade.ProfitLoss;
            }
        }

        private static decimal StandardDeviation(IEnumerable<decimal> values)
        {
            int count = values.Count();
            if (count == 0)
            {
                return 0;
            }

            //Compute the Average
            decimal avg = values.Average();

            //Perform the Sum of (value-avg)^2
            decimal sum = values.Sum(d => (d - avg) * (d - avg));

            //Put it all together
            return (decimal)Math.Sqrt((double)(sum / count));
        }
    }
}