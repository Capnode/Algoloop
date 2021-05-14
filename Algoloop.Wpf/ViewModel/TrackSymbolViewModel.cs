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

namespace Algoloop.Wpf.ViewModel
{
    public class TrackSymbolViewModel
    {
        readonly List<Trade> _trades = new List<Trade>();
        private readonly Symbol _symbol;

        public TrackSymbolViewModel(Symbol symbol)
        {
            _symbol = symbol;
        }

        /// <summary>
        /// The symbol of the traded instrument
        /// </summary>
        public string Symbol => _symbol.Value;

        /// <summary>
        /// The market of the traded instrument
        /// </summary>
        public string Market => _symbol.ID.Market;

        /// <summary>
        /// The security type of the traded instrument
        /// </summary>
        public SecurityType Security => _symbol.ID.SecurityType;

        /// <summary>
        /// The type of the traded instrument
        /// </summary>
        public string Type => _symbol.SecurityType.ToString();

        public decimal Score { get; private set; }

        /// <summary>
        /// Total profit to max drawdown ratio
        /// </summary>
        public decimal RoMaD { get; private set; }

        /// <summary>
        /// The number of trades
        /// </summary>
        public int Trades => _trades.Count;

        /// <summary>
        /// The gross profit/loss of the trades (as account currency)
        /// </summary>
        public decimal NetProfit { get; private set; }

        /// <summary>
        /// The maximum drawdown of the trades (as account currency)
        /// </summary>
        public decimal Drawdown { get; private set; }

        /// <summary>
        /// Longest drawdown period
        /// </summary>
        public TimeSpan DrawdownPeriod { get; private set; }

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

            decimal netProfit = _trades.Sum(m => m.ProfitLoss - m.TotalFees);
            NetProfit = netProfit.RoundToSignificantDigits(2);

            // Calculate Return over Max Drawdown Ratio
            decimal drawdown = TrackViewModel.MaxDrawdown(_trades, out TimeSpan period);
            Drawdown = drawdown.RoundToSignificantDigits(2);
            DrawdownPeriod = period;
            decimal roMaD = drawdown == 0 ? 0 : netProfit / -drawdown;
            RoMaD = roMaD.RoundToSignificantDigits(4);

            // Calculate score
            double score = TrackViewModel.CalculateScore(_trades);
            Score = ((decimal)score).RoundToSignificantDigits(4);
        }
    }
}
