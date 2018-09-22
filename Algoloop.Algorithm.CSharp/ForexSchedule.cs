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

using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Parameters;
using System;

namespace Algoloop.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm demonstrating FOREX asset types and requesting history on them in bulk. As FOREX uses
    /// QuoteBars you should request slices or
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="history and warm up" />
    /// <meta name="tag" content="history" />
    /// <meta name="tag" content="forex" />
    public class ForexSchedule : QCAlgorithm
    {
        [Parameter("symbols")]
        private string __symbols = "EURUSD";
        private string _symbol;

        [Parameter("resolution")]
        private string __resolution;
        private Resolution _resolution = Resolution.Minute;

        [Parameter("market")]
        private string __market = Market.FXCM;

        [Parameter("startdate")]
        private string __startdate = "20180101 00:00:00";
        private DateTime _startdate;

        [Parameter("enddate")]
        private string __enddate = "20180901 00:00:00";
        private DateTime _enddate;

        [Parameter("cash")]
        private string __cash = "100000";
        private int _cash;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            _startdate = DateTime.Parse(__startdate);
            _enddate = DateTime.Parse(__enddate);
            _symbol = __symbols.Split(';')[0];
            Enum.TryParse(__resolution, out _resolution);
            _cash = int.Parse(__cash);

            SetStartDate(_startdate);
            SetEndDate(_enddate);
            SetCash(_cash);
            AddForex(_symbol, _resolution, __market);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (Portfolio.Invested)
            {
                if (Time.Minute/10 % 2 == 1)
                {
                    Log($"Close {string.Join(", ", data.Values)}");
                    Liquidate("EURUSD");
                }
            }
            else
            {
                if (Time.Minute/10 % 2 == 0)
                {
                    Log($"Open {string.Join(", ", data.Values)}");
                    SetHoldings("EURUSD", .5);
                }
            }
        }
    }
}