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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Algoloop.ViewModel.Internal
{
    internal static class Converters
    {
        public static Candle ToCandle(BaseData data, Security security = default)
        {
            if (security == default)
            {
                security = new Security
                {
                    Id = data.Symbol.ID.Symbol
                };
            }

            if (data is TradeBar trade)
            {
                return new TimeFrameCandle
                {
                    Security = security,
                    TimeFrame = trade.Period,
                    OpenTime = trade.Time,
                    HighTime = trade.Time,
                    LowTime = trade.Time,
                    CloseTime = trade.Time,
                    OpenPrice = trade.Open,
                    HighPrice = trade.High,
                    LowPrice = trade.Low,
                    ClosePrice = trade.Close,
                    OpenVolume = 0,
                    HighVolume = 0,
                    LowVolume = 0,
                    CloseVolume = trade.Volume,
                    BuildFrom = DataType.Ticks,
                    State = CandleStates.Finished
                };
            }
            else if (data is QuoteBar quote)
            {
                return new TimeFrameCandle
                {
                    Security = security,
                    TimeFrame = quote.Period,
                    OpenTime = quote.Time,
                    HighTime = quote.Time,
                    LowTime = quote.Time,
                    CloseTime = quote.Time,
                    OpenPrice = quote.Open,
                    HighPrice = quote.High,
                    LowPrice = quote.Low,
                    ClosePrice = quote.Close,
                    OpenVolume = 0,
                    HighVolume = 0,
                    LowVolume = 0,
                    CloseVolume = 0,
                    BuildFrom = DataType.Ticks,
                    State = CandleStates.Finished
                };
            }

            throw new NotImplementedException("Unknown BaseData subclass");
        }

        public static IEnumerable<Candle> ToCandles(this IEnumerable<BaseData> data)
        {
            BaseData first = data.FirstOrDefault();
            if (first == default) return default;
            var security = new Security
            {
                Id = first.Symbol.ID.Symbol
            };
            return data.Select(m => ToCandle(m, security));
        }

        public static System.Windows.Media.Color ToMediaColor(System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromRgb(color.R, color.G, color.B);
        }
    }
}
