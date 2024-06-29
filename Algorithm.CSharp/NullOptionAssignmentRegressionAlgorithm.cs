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
 *
*/

using System.Collections.Generic;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm assering we can disable automatic option assignment
    /// </summary>
    public class NullOptionAssignmentRegressionAlgorithm : OptionAssignmentRegressionAlgorithm
    {
        public override void Initialize()
        {
            SetSecurityInitializer((security) =>
            {
                var option = security as Option;
                option?.SetOptionAssignmentModel(new NullOptionAssignmentModel());
            });

            base.Initialize();
        }

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "4"},
            {"Average Win", "9.48%"},
            {"Average Loss", "-16.73%"},
            {"Compounding Annual Return", "-25.790%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "-0.478"},
            {"Start Equity", "100000"},
            {"End Equity", "99538"},
            {"Net Profit", "-0.462%"},
            {"Sharpe Ratio", "-6.902"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "95.713%"},
            {"Loss Rate", "67%"},
            {"Win Rate", "33%"},
            {"Profit-Loss Ratio", "0.57"},
            {"Alpha", "-0.009"},
            {"Beta", "-0.023"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "10.521"},
            {"Tracking Error", "0.018"},
            {"Treynor Ratio", "0.211"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "26.71%"},
            {"OrderListHash", "1a992ecaf12fab7862144531205c016a"}
        };
    }
}
