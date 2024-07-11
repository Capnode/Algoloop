/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    public class OptionExpiryDateOnHolidayCase : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string UnderlyingTicker = "SPY";
        public Symbol Underlying { get; init; } = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Equity, Market.USA);
        private readonly Symbol _optionSymbol = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Option, Market.USA);
        private OptionContract _optionContract;
        private List<Delisting> _delistings = new List<Delisting>();

        public override void Initialize()
        {
            SetStartDate(2014, 4, 15);
            SetEndDate(2014, 4, 22);
            SetCash(startingCash: 100000);

            var equity = AddEquity(UnderlyingTicker);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            var option = AddOption(UnderlyingTicker);
            option.SetFilter(f => f.Expiration(TimeSpan.Zero, TimeSpan.FromDays(30)));
        }

        public override void OnData(Slice slice)
        {
            OptionChain chain;
            if (!Portfolio.Invested && IsMarketOpen(_optionSymbol))
            {
                if (slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                {
                    _optionContract = chain.FirstOrDefault(c => c.Expiry.Date == new DateTime(2014, 04, 19) && c.OpenInterest > 0);
                    if (_optionContract != null) MarketOrder(_optionContract.Symbol, 1);
                }
            }

            Delisting delisting;
            if (slice.Delistings.TryGetValue(_optionContract.Symbol, out delisting))
            {
                Log(delisting.ToString());
                _delistings.Add(delisting);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!(_delistings.Count == 2 &&
                  _delistings.Any(d => d.Type == DelistingType.Warning) &&
                  _delistings.Any(d => d.Type == DelistingType.Delisted)))
            {
                throw new RegressionTestException($"Option contract {_optionContract.Symbol} was not correctly delisted.");
            }

            if (_delistings.FirstOrDefault(d => d.Type == DelistingType.Warning).EndTime.Date !=
                new DateTime(2014, 04, 16))
            {
                throw new RegressionTestException($"Option contract {_optionContract.Symbol} delisting warning was not fired the right date.");
            }

            if (_delistings.FirstOrDefault(d => d.Type == DelistingType.Delisted).EndTime.Date !=
                new DateTime(2014, 04, 17))
            {
                throw new RegressionTestException($"Option contract {_optionContract.Symbol} was not delisted the right date.");
            }

            if (Portfolio[_optionContract.Symbol].Invested)
            {
                throw new RegressionTestException($"Option contract {_optionContract.Symbol} was not wasn't liquidated as part of delisting.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 0;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.54%"},
            {"Compounding Annual Return", "23.156%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "-1"},
            {"Net Profit", "0.448%"},
            {"Sharpe Ratio", "15.59"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.171"},
            {"Beta", "-0.65"},
            {"Annual Standard Deviation", "0.01"},
            {"Annual Variance", "0"},
            {"Information Ratio", "13.971"},
            {"Tracking Error", "0.01"},
            {"Treynor Ratio", "-0.248"},
            {"Total Fees", "$1.00"}
        };
    }
}
