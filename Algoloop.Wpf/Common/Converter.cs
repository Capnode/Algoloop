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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Algoloop.Wpf.Common
{
    public static class Converter
    {
        public static Candle ToCandle(BaseData data, Security security = default)
        {
            if (security == default)
            {
                security = new Security
                {
                    Id = data.Symbol.ID.Symbol,
                    PriceStep = 0.01m,
                    Board = ExchangeBoard.Test
                };
            }

            if (data is TradeBar trade)
            {
                return new TimeFrameCandle
                {
                    Security = security,
                    TimeFrame = trade.Period,
                    OpenTime = trade.Time,
                    OpenPrice = trade.Open,
                    HighPrice = trade.High,
                    LowPrice = trade.Low,
                    ClosePrice = trade.Low
                };
            }
            else if (data is QuoteBar quote)
            {
                return new TimeFrameCandle
                {
                    TimeFrame = quote.Period,
                    Security = security,
                    OpenTime = quote.Time,
                    OpenPrice = quote.Open,
                    HighPrice = quote.High,
                    LowPrice = quote.Low,
                    ClosePrice = quote.Low
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
                Id = first.Symbol.ID.Symbol,
                PriceStep = 0.01m,
                Board = ExchangeBoard.Test
            };
            return data.Select(m => ToCandle(m, security));
        }
    }
}
