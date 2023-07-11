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

using Algoloop.Algorithm.CSharp.Model;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders.Fees;
using QuantConnect.Parameters;
using QuantConnect.Securities;
using System.Globalization;

namespace Algoloop.Algorithm.CSharp.Algo
{
    public class TrendbandAlgo : QCAlgorithm
    {
        private enum Mode { Undefined, Trend, Contrarian };

        [Parameter("symbols")]
        private readonly string _symbols = null;
        private Symbol _symbol;

        [Parameter("security")]
        private readonly string _security = null;

        [Parameter("resolution")]
        private readonly string _resolution = null;

        [Parameter("market")]
        private readonly string _market = null;

        [Parameter("Mode")]
        private readonly string _mode = "Trend";
        private Mode __mode = Mode.Undefined;

        [Parameter("Fee")]
        private readonly string _fee = "0";

        [Parameter("Period")]
        private readonly string _period = "8";

        [Parameter("Minutes")]
        private readonly string _minutes = "60";
        private int __minutes;

        [Parameter("Size")]
        private readonly string _size = "0.25";
        private double __size;

        public Maximum UpperBand { get; private set; }
        public Minimum LowerBand { get; private set; }

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash
        /// and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            if (string.IsNullOrEmpty(_symbols)) throw new ArgumentNullException(nameof(_symbols));
            if (string.IsNullOrEmpty(_security)) throw new ArgumentNullException(nameof(_security));
            if (string.IsNullOrEmpty(_resolution)) throw new ArgumentNullException(nameof(_resolution));
            if (string.IsNullOrEmpty(_market)) throw new ArgumentNullException(nameof(_market));

            SecurityType securityType = (SecurityType)Enum.Parse(typeof(SecurityType), _security);
            Resolution resolution = (Resolution)Enum.Parse(typeof(Resolution), _resolution);
            decimal fee = decimal.Parse(_fee, CultureInfo.InvariantCulture);
            EnableAutomaticIndicatorWarmUp = true;
            MarketHoursDatabase.Entry entry = MarketHoursDatabase.GetEntry(_market, (string)null, securityType);
            SetTimeZone(entry.DataTimeZone);
            FeeModel feeModel = fee < 1 ? new PercentFeeModel(fee) : new ConstantFeeModel(fee);
            SetSecurityInitializer(security =>
            {
                security.FeeModel = feeModel;
                security.FillModel = new TouchFill();
            });

            Log($"Timezone {TimeZone}");
            string symbol = _symbols.Split(';')[0];
            var forex = AddForex(symbol, resolution, _market);
            _symbol = forex.Symbol;

            if (!Enum.TryParse(_mode, out __mode)) throw new ArgumentException(nameof(_mode));
            int period = int.Parse(_period, CultureInfo.InvariantCulture);
            __minutes = int.Parse(_minutes, CultureInfo.InvariantCulture);
            __size = double.Parse(_size, CultureInfo.InvariantCulture);
            Log($"Trendband {__mode}: {_symbol} {_resolution} {period} {__minutes} {__size}");

            if (__minutes > 0)
            {
                Consolidate<QuoteBar>(_symbol, TimeSpan.FromMinutes(__minutes), BarHandler);
            }
            UpperBand = new Maximum("UpperBand", period);
            LowerBand = new Minimum("LowerBand", period);
        }

        public override void OnData(Slice slice)
        {
            if (slice == default) throw new ArgumentNullException(nameof(slice));
            if (!IsMarketOpen(_symbol)) return;
            if (__minutes > 0) return;

            QuoteBar quote = slice.QuoteBars[_symbol.Value];
            //Log(string.Format(
            //    CultureInfo.InvariantCulture,
            //    "{0:u}: {1} {2} {3} {4} {5} {6} {7} {8}",
            //    quote.Time,
            //    quote.Ask.Open,
            //    quote.Bid.Open,
            //    quote.Ask.High,
            //    quote.Bid.High,
            //    quote.Ask.Low,
            //    quote.Bid.Low,
            //    quote.Ask.Close,
            //    quote.Bid.Close));

            switch (__mode)
            {
                case Mode.Trend:
                    Trend(quote);
                    break;
                case Mode.Contrarian:
                    Contrarian(quote);
                    break;
            }
        }

        private void BarHandler(QuoteBar quote)
        {
            if (!IsMarketOpen(_symbol)) return;

            //Log(string.Format(
            //    CultureInfo.InvariantCulture,
            //    "{0} {1} {2} {3} {4} {5} {6} {7}",
            //    quote.Ask.Open,
            //    quote.Bid.Open,
            //    quote.Ask.High,
            //    quote.Bid.High,
            //    quote.Ask.Low,
            //    quote.Bid.Low,
            //    quote.Ask.Close,
            //    quote.Bid.Close));

            switch (__mode)
            {
                case Mode.Trend:
                    Trend(quote);
                    break;
                case Mode.Contrarian:
                    Contrarian(quote);
                    break;
            }
        }

        private void Trend(QuoteBar quote)
        {
            if (UpperBand.IsReady && LowerBand.IsReady)
            {
//                Log($"{UpperBand} {LowerBand}");
                SecurityHolding holding = Portfolio[_symbol];
                bool aboveBands = quote.Ask.Close > UpperBand && quote.Bid.Close > LowerBand;
                bool belowBands = quote.Ask.Close < UpperBand && quote.Bid.Close < LowerBand;

                if (holding.IsLong)
                {
                    if (!aboveBands)
                    {
                        Liquidate(_symbol);
//                        Log($"Liquidate");
                    }
                }
                else if (holding.IsShort)
                {
                    if (!belowBands)
                    {
                        Liquidate(_symbol);
//                        Log($"Liquidate");
                    }
                }
                else if (aboveBands && belowBands)
                {
                    Log($"*** above and below ***");
                }
                else if (aboveBands)
                {
                    SetHoldings(_symbol, __size);
//                    Log($"Long");
                }
                else if (belowBands)
                {
                    SetHoldings(_symbol, -__size);
//                    Log($"Short");
                }
            }

            UpperBand.Update(new IndicatorDataPoint(quote.Time, quote.Ask.Low));
            LowerBand.Update(new IndicatorDataPoint(quote.Time, quote.Bid.High));
        }

        private void Contrarian(QuoteBar quote)
        {
            if (UpperBand.IsReady && LowerBand.IsReady)
            {
//                Log($"{UpperBand} {LowerBand}");
                SecurityHolding holding = Portfolio[_symbol];
                bool aboveBands = quote.Ask.Close > UpperBand && quote.Bid.Close > LowerBand;
                bool belowBands = quote.Ask.Close < UpperBand && quote.Bid.Close < LowerBand;

                if (holding.IsLong)
                {
                    if (!aboveBands)
                    {
                        Liquidate(_symbol);
//                        Log($"Liquidate");
                    }
                }
                else if (holding.IsShort)
                {
                    if (!belowBands)
                    {
                        Liquidate(_symbol);
//                        Log($"Liquidate");
                    }
                }
                else if (aboveBands && belowBands)
                {
                    Log($"*** aboveBands and belowBands ***");
                }
                else if (belowBands)
                {
                    SetHoldings(_symbol, __size);
//                    Log($"Long");
                }
                else if (aboveBands)
                {
                    SetHoldings(_symbol, -__size);
//                    Log($"Short");
                }
            }

            UpperBand.Update(new IndicatorDataPoint(quote.Time, quote.Bid.Low));
            LowerBand.Update(new IndicatorDataPoint(quote.Time, quote.Ask.High));
        }
    }
}
