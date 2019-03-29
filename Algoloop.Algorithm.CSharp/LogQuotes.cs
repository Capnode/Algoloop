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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Parameters;
using System;
using System.Globalization;

namespace Algoloop.Algorithm.CSharp
{
    public class LogQuotes : QCAlgorithm
    {
        [Parameter("symbols")]
        private string _symbols = "EURUSD";
        private Symbol _symbol;

        [Parameter("resolution")]
        private string _resolution = "Hour";

        [Parameter("market")]
        private string _market = Market.FXCM;

        [Parameter("startdate")]
        private string _startdate = "2018-01-01 00:00:00";

        [Parameter("enddate")]
        private string _enddate = "2018-10-30 00:00:00";

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash
        /// and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Standard parameters
            SetStartDate(DateTime.Parse(_startdate, CultureInfo.InvariantCulture));
            SetEndDate(DateTime.Parse(_enddate, CultureInfo.InvariantCulture));

            Resolution resolution = Resolution.Hour;
            Enum.TryParse(_resolution, out resolution);
            string symbol = _symbols.Split(';')[0];
            var forex = AddForex(symbol, resolution, _market);
            _symbol = forex.Symbol;
        }

        public override void OnData(Slice slice)
        {
            QuoteBar quote = slice.QuoteBars[_symbol.Value];
            Log(string.Format(
                CultureInfo.InvariantCulture,
                "{0:u}: {1} {2} {3} {4} {5}",
                quote.Time,
                quote.Ask.Open,
                quote.Bid.Open,
                quote.Ask.Close,
                quote.Bid.Close,
                IsMarketOpen(_symbol)));
        }
    }
}