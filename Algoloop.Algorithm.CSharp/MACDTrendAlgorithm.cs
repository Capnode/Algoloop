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
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Parameters;
using QuantConnect.Securities.Forex;

namespace Algoloop.Algorithm.CSharp
{
    /// <summary>
    /// Simple indicator demonstration algorithm of MACD
    /// </summary>
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="indicator classes" />
    /// <meta name="tag" content="plotting indicators" />
    public class MACDTrendAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        [Parameter("fast-period")]
        private int _fastPeriod = 12;

        [Parameter("slow-period")]
        private int _slowPeriod = 26;

        [Parameter("signal-period")]
        private int _signalPeriod = 9;

        [Parameter("symbols")]
        private string _symbols = "EURUSD";

        [Parameter("resolution")]
        private string _resolution = "Minute";

        [Parameter("market")]
        private string _market = "fxcm";

        [Parameter("startdate")]
        private string __startdate = "20180101 00:00:00";

        [Parameter("enddate")]
        private string __enddate = "20180901 00:00:00";

        private DateTime _previous;
        private MovingAverageConvergenceDivergence _macd;
        private string _symbol;
        private Forex _forex;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            DateTime startdate;
            if (DateTime.TryParse(__startdate, out startdate))
            {
                SetStartDate(startdate);
            }

            DateTime enddate;
            if (DateTime.TryParse(__enddate, out enddate))
            {
                SetEndDate(enddate);
            }

            _symbol = _symbols.Split(';')[0];

            Resolution res;
            if (Enum.TryParse(_resolution, out res))
            {
                _forex = AddForex(_symbol, res, _market);
            }

            // define our daily macd(12,26) with a 9 day signal
            _macd = MACD(_forex.Symbol, _fastPeriod, _slowPeriod, _signalPeriod, MovingAverageType.Exponential, Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            //            Log($"OnData() {Time}");
            // only once per day
            if (_previous.Date == Time.Date) return;

            if (!_macd.IsReady) return;

            var holding = Portfolio[_symbol];

            var signalDeltaPercent = _macd - _macd.Signal;
            var tolerance = 0m;

            // if our macd is greater than our signal, then let's go long
            if (holding.Quantity <= 0 && signalDeltaPercent > tolerance) // 0.01%
            {
                // longterm says buy as well
                SetHoldings(_forex.Symbol, 1.0);
            }
            // of our macd is less than our signal, then let's go short
            else if (holding.Quantity >= 0 && signalDeltaPercent < -tolerance)
            {
                SetHoldings(_forex.Symbol, -1.0);
            }

            // plot both lines
            Plot("MACD", _macd, _macd.Signal);
            Plot(_symbol, "Open", data[_symbol].Open);
            Plot(_symbol, _macd.Fast, _macd.Slow);

            _previous = Time;
        }

        public override void OnEndOfAlgorithm()
        {
            Log($"OnEndOfAlgorithm() {Time}");
            base.OnEndOfAlgorithm();
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "84"},
            {"Average Win", "4.78%"},
            {"Average Loss", "-4.16%"},
            {"Compounding Annual Return", "2.958%"},
            {"Drawdown", "34.800%"},
            {"Expectancy", "0.228"},
            {"Net Profit", "37.837%"},
            {"Sharpe Ratio", "0.297"},
            {"Loss Rate", "43%"},
            {"Win Rate", "57%"},
            {"Profit-Loss Ratio", "1.15"},
            {"Alpha", "0.107"},
            {"Beta", "-3.51"},
            {"Annual Standard Deviation", "0.124"},
            {"Annual Variance", "0.015"},
            {"Information Ratio", "0.136"},
            {"Tracking Error", "0.125"},
            {"Treynor Ratio", "-0.011"},
            {"Total Fees", "$443.50"}
        };
    }
}
