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
using QuantConnect.Data.Market;
using QuantConnect.Parameters;
using QuantConnect.Securities;
using System;
using System.Globalization;

namespace Capnode.Algorithm.CSharp
{
    public class Tide : QCAlgorithm
    {
        [Parameter("symbols")]
        protected string _symbols = "EURUSD";
        private string _symbol;

        [Parameter("resolution")]
        protected string _resolution = "Hour";

        [Parameter("market")]
        protected string _market = Market.FXCM;

        [Parameter("startdate")]
        protected string _startdate = "2018-01-01 00:00:00";

        [Parameter("enddate")]
        protected string _enddate = "2018-10-30 00:00:00";

        [Parameter("cash")]
        protected string _cash = "100000";

        [Parameter("OpenHourLong")]
        private string _openHourLong = "1";
        private TimeSpan _openTimeLong;

        [Parameter("CloseHourLong")]
        private string _closeHourLong = "3";
        private TimeSpan _closeTimeLong;

        [Parameter("OpenHourShort")]
        private string _openHourShort = "1";
        private TimeSpan _openTimeShort;

        [Parameter("CloseHourShort")]
        private string _closeHourShort = "3";
        private TimeSpan _closeTimeShort;

        [Parameter("SymbolIndex")]
        private string _symbolIndex = "0";
        private int __symbolIndex = 0;

        [Parameter("Size")]
        private string _size = "1";
        private double __size = 0;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash
        /// and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetTimeZone(NodaTime.DateTimeZone.Utc);
            Log($"Timezone {TimeZone}");

            // Standard parameters
            SetStartDate(DateTime.Parse(_startdate, CultureInfo.InvariantCulture));
            SetEndDate(DateTime.Parse(_enddate, CultureInfo.InvariantCulture));
            SetCash(int.Parse(_cash));

            Resolution resolution = Resolution.Hour;
            Enum.TryParse(_resolution, out resolution);
            int.TryParse(_symbolIndex, out __symbolIndex);
            _symbol = _symbols.Split(';')[__symbolIndex];
            AddForex(_symbol, resolution, _market);

            // Algorithm parameters
            _openTimeLong = TimeSpan.FromHours(double.Parse(_openHourLong));
            _closeTimeLong = TimeSpan.FromHours(double.Parse(_closeHourLong));
            _openTimeShort = TimeSpan.FromHours(double.Parse(_openHourShort));
            _closeTimeShort = TimeSpan.FromHours(double.Parse(_closeHourShort));
            __size = double.Parse(_size, CultureInfo.InvariantCulture);

            Log($"Tide {_openTimeLong.Hours} {_closeTimeLong.Hours} {_openTimeShort.Hours} {_closeTimeShort.Hours} {__symbolIndex} {__size}");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm.
        /// Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            if (!IsMarketOpen(_symbol))
                return;

            SecurityHolding holding = Portfolio[_symbol];
            TimeSpan now = Time.TimeOfDay;
            bool longInHours = InHours(_openTimeLong, now, _closeTimeLong);
            bool shortInHours = InHours(_openTimeShort, now, _closeTimeShort);

            if (holding.IsLong)
            {
                if (!longInHours)
                {
                    Liquidate(_symbol);
                }
                else
                {
                    return;
                }
            }
            else if (holding.IsShort)
            {
                if (!shortInHours)
                {
                    Liquidate(_symbol);
                }
                else
                {
                    return;
                }
            }

            if (longInHours)
            {
                SetHoldings(_symbol, __size);
            }
            else if (shortInHours)
            {
                SetHoldings(_symbol, -__size);
            }
        }

        private bool InHours(TimeSpan open, TimeSpan now, TimeSpan close)
        {
            if (open == close)
            {
                return false;
            }

            if (open < close)
            {
                return open <= now && now < close;
            }
            else
            {
                return now < close || open <= now;
            }
        }
    }
}