/*
 * Copyright 2022 Capnode AB
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

using QuantConnect.Data.Market;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;

namespace Algoloop.Wpf.Brokerages
{
    public static class MarketHelper
    {

        public static string ValidateSymbolName(string name)
        {
            string symbol = name.Replace(" ", "-");
            return symbol;
        }

        public static void ValidateTadeBars(this List<TradeBar> bars)
        {
            ValidateTimeGaps(bars);
            ValidatePrices(bars);
        }

        private static void ValidateTimeGaps(List<TradeBar> bars)
        {
            var time0 = DateTime.MinValue;
            var array = bars.ToArray();
            bars.Clear();
            foreach (TradeBar bar in array)
            {
                bars.Add(bar);
                TimeSpan elapsed = bar.Time - time0;
                if (time0 > DateTime.MinValue && elapsed > TimeSpan.FromDays(30))
                {
                    Log.Trace($"Fixed timegap error at {bar.Time.ToShortDateString()} in {bar}");
                    bars.Clear();
                }

                time0 = bar.Time;
            }
        }

        private static void ValidatePrices(List<TradeBar> bars)
        {
            decimal price = 0;
            foreach (TradeBar bar in bars)
            {
                bool fix = false;
                var bar0 = new TradeBar(bar);
                if (bar.Close == 0)
                {
                    fix = true;
                    bar.Close = price;
                }
                if (bar.Open == 0)
                {
                    fix = true;
                    bar.Open = bar.Close;
                }
                if (bar.Low == 0)
                {
                    fix = true;
                    bar.Low = Math.Min(bar.Open, bar.Close);
                }
                if (bar.High == 0)
                {
                    fix = true;
                    bar.High = Math.Max(bar.Open, bar.Close);
                }
                if (bar.Low > bar.Close)
                {
                    fix = true;
                    bar.Low = Math.Min(bar.Open, bar.Close);
                }
                if (bar.High < bar.Close)
                {
                    fix = true;
                    bar.High = Math.Max(bar.Open, bar.Close);
                }
                if (fix)
                {
                    Log.Trace($"Fixed price error at {bar0.Time.ToShortDateString()} in {bar0} ===> {bar}");
                }

                price = bar.Close;
            }
        }
    }
}
