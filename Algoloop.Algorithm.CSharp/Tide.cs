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
using System.Globalization;

namespace Algoloop.Algorithm.CSharp
{
    public class Tide : QCAlgorithm
    {
        private enum MarketSide { Buy, Sell };

        [Parameter("symbols")]
        private string _symbols = "EURUSD";
        private string _symbol;

        [Parameter("resolution")]
        private string _resolution = "Minute";

        [Parameter("market")]
        private string _market = Market.FXCM;

        [Parameter("startdate")]
        private string _startdate = "2018-01-01 00:00:00";

        [Parameter("enddate")]
        private string _enddate = "2018-09-01 00:00:00";

        [Parameter("cash")]
        private string _cash = "100000";

        [Parameter("opentime")]
        private string _opentime = "01:00:00";
        private DateTime __opentime;

        [Parameter("closetime")]
        private string _closetime = "03:00:00";
        private DateTime __closetime;

        [Parameter("side")]
        private string _side = "Buy";
        private MarketSide __side = MarketSide.Buy;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash
        /// and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Standard parameters
            SetStartDate(DateTime.Parse(_startdate, CultureInfo.InvariantCulture));
            SetEndDate(DateTime.Parse(_enddate, CultureInfo.InvariantCulture));
            SetCash(int.Parse(_cash));

            Resolution resolution = Resolution.Hour;
            Enum.TryParse(_resolution, out resolution);
            _symbol = _symbols.Split(';')[0];
            AddForex(_symbol, resolution, _market);

            // Algorithm parameters
            __opentime = DateTime.Parse(_opentime, CultureInfo.InvariantCulture);
            __closetime = DateTime.Parse(_closetime, CultureInfo.InvariantCulture);
            Enum.TryParse(_side, out __side);

            SetTimeZone(NodaTime.DateTimeZone.Utc);
            Log($"Timezone {TimeZone}");
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
                if (Time.TimeOfDay >= __closetime.TimeOfDay || Time.TimeOfDay < __opentime.TimeOfDay)
                {
                    Log($"Close {_symbol}");
                    Liquidate(_symbol);
                }
            }
            else if (__side.Equals(MarketSide.Buy))
            {
                if (Time.TimeOfDay >= __opentime.TimeOfDay && Time.TimeOfDay < __closetime.TimeOfDay)
                {
                    Log($"Buy {_symbol}");
                    SetHoldings(_symbol, 0.5);
                }
            }
            else if (__side.Equals(MarketSide.Sell))
            {
                if (Time.TimeOfDay >= __opentime.TimeOfDay && Time.TimeOfDay < __closetime.TimeOfDay)
                {
                    Log($"Sell {_symbol}");
                    SetHoldings(_symbol, -0.5);
                }
            }
        }
    }
}