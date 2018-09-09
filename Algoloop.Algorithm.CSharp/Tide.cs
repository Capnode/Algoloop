/*
 * Copyright 2018 Capnode AB
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
using QuantConnect.Data.Market;
using QuantConnect.Parameters;
using System;

namespace Algoloop.Algorithm.CSharp
{
    public class Tide : QCAlgorithm
    {
        private enum MarketSide { Buy, Sell };

        [Parameter("symbols")]
        private string __symbols = "EURUSD";
        private string _symbol;

        [Parameter("resolution")]
        private string __resolution;
        private Resolution _resolution = Resolution.Hour;

        [Parameter("market")]
        private string __market = "fxcm";

        [Parameter("startdate")]
        private string __startdate = "20180101 00:00:00";
        private DateTime _startdate;

        [Parameter("enddate")]
        private string __enddate = "20180901 00:00:00";
        private DateTime _enddate;

        [Parameter("opentime")]
        private string __opentime = "01:00:00";
        private DateTime _opentime;

        [Parameter("closetime")]
        private string __closetime = "03:00:00";
        private DateTime _closetime;

        [Parameter("side")]
        private string __side;
        private MarketSide _side = MarketSide.Buy;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash
        /// and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            _startdate = DateTime.Parse(__startdate);
            _enddate = DateTime.Parse(__enddate);
            _opentime = DateTime.Parse(__opentime);
            _closetime = DateTime.Parse(__closetime);
            _symbol = __symbols.Split(';')[0];
            Enum.TryParse(__resolution, out _resolution);
            Enum.TryParse(__side, out _side);

            SetStartDate(_startdate);
            SetEndDate(_enddate);
            AddForex(_symbol, _resolution, __market);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm.
        /// Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            if (Portfolio.Invested)
            {
                if (Time.TimeOfDay >= _closetime.TimeOfDay || Time.TimeOfDay < _opentime.TimeOfDay)
                {
                    Log($"Close {_symbol}");
                    Liquidate(_symbol);
                }
            }
            else if (_side.Equals(MarketSide.Buy))
            {
                if (Time.TimeOfDay >= _opentime.TimeOfDay && Time.TimeOfDay < _closetime.TimeOfDay)
                {
                    Log($"Buy {_symbol}");
                    SetHoldings(_symbol, 0.5);
                }
            }
            else if (_side.Equals(MarketSide.Sell))
            {
                if (Time.TimeOfDay >= _opentime.TimeOfDay && Time.TimeOfDay < _closetime.TimeOfDay)
                {
                    Log($"Sell {_symbol}");
                    SetHoldings(_symbol, -0.5);
                }
            }
        }
    }
}
