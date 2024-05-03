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

using Algoloop.Wpf.Model;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Messages;
using StockSharp.Xaml.Charting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Algoloop.Wpf.ViewModels.Views.Internal.Lean
{
    internal static class StockSharpExtensions
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
                    HighVolume = trade.Volume,
                    LowVolume = 0,
                    CloseVolume = trade.Volume,
                    TotalVolume = trade.Volume,
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
                    TotalVolume = 0,
                    BuildFrom = DataType.Ticks,
                    State = CandleStates.Finished
                };
            }
            else if (data is Tick tick)
            {
                return new TimeFrameCandle
                {
                    Security = security,
                    OpenTime = tick.Time,
                    HighTime = tick.Time,
                    LowTime = tick.Time,
                    CloseTime = tick.Time,
                    OpenPrice = tick.Price,
                    HighPrice = tick.Price,
                    LowPrice = tick.Price,
                    ClosePrice = tick.Price,
                    OpenVolume = 0,
                    HighVolume = 0,
                    LowVolume = 0,
                    CloseVolume = 0,
                    TotalVolume = 0,
                    BuildFrom = DataType.Ticks,
                    State = CandleStates.Finished
                };
            }

            throw new NotImplementedException($"Unknown BaseData subclass: {data.GetType()}");
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

        public static IChartElement Clear(this IChartElement element)
        {
            element.FullTitle = null;
            return element;
        }

        public static IChartElement Update(this IChartElement element, BaseSeries series)
        {
            if (element is IChartLineElement lineElement)
            {
                lineElement.FullTitle = series.Name;
                lineElement.Style = ToStyle(series.SeriesType);
                lineElement.Color = ToRGB(series);
                lineElement.AntiAliasing = false;
                lineElement.IsLegend = true;
                lineElement.ShowAxisMarker = true;
                return lineElement;
            }

            if (element is IChartCandleElement candleElement)
            {
                candleElement.FullTitle = series.Name;
                candleElement.DrawStyle = ToCandleDrawStyle(series.SeriesType);
                candleElement.UpBorderColor = ToRGB(series);
                candleElement.DownBorderColor = ToRGB(series);
                candleElement.AntiAliasing = false;
                candleElement.IsLegend = true;
                candleElement.ShowAxisMarker = true;
                return candleElement;
            }

            return null;
        }

        public static IChartElement Update(this IChartElement element, SymbolModel symbol)
        {
            if (element is IChartCandleElement candleElement)
            {
                candleElement.FullTitle = symbol.Name;
                candleElement.DrawStyle = ChartCandleDrawStyles.CandleStick;
                candleElement.UpBorderColor = Color.Black;
                candleElement.DownBorderColor = Color.Black;
                candleElement.AntiAliasing = false;
                candleElement.IsLegend = true;
                candleElement.ShowAxisMarker = true;
                return candleElement;
            }

            return null;
        }

        public static System.Windows.Media.Color ToMediaColor(Color color)
        {
            if (color.IsEmpty)
            {
                return System.Windows.Media.Colors.Black;
            }

            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private static Color ToRGB(BaseSeries baseSeries)
        {
            if ( baseSeries is Series series)
            {
                return Color.FromArgb(series.Color.R, series.Color.G, series.Color.B);
            }

            return Color.Black;
        }

        public static IEnumerable<EquityData> ToEquityData(this BaseSeries series)
        {
            return series.Values.Select(m => ToEquityData(m));
        }

        private static EquityData ToEquityData(ISeriesPoint point)
        {
            if (point is Candlestick candlestick)
            {
                return new EquityData
                {
                    Time = candlestick.Time.ToLocalTime(),
                    Value = RoundLarge(candlestick.Close)
                };
            }

            if (point is ChartPoint chartPoint)
            {

                return new EquityData
                {
                    Time = Time.UnixTimeStampToDateTime(chartPoint.x).ToLocalTime(),
                    Value = RoundLarge(chartPoint.y)
                };
            }

            return null;
        }

        public static Security ToSecurity(this SymbolModel symbolModel)
        {
            return new Security
            {
                Id = symbolModel.Name,
                Code = symbolModel.Id,
                Type = symbolModel.Security.ToSecurityTypes(),
                PriceStep = 0.01m,
                Board = ExchangeBoard.Associated,
            };
        }

        private static ChartIndicatorDrawStyles ToStyle(SeriesType seriesType)
        {
            switch (seriesType)
            {
                case SeriesType.Line: return ChartIndicatorDrawStyles.Line;
                case SeriesType.Pie: return ChartIndicatorDrawStyles.Bubble;
                case SeriesType.Scatter: return ChartIndicatorDrawStyles.Dot;
                case SeriesType.Bar: return ChartIndicatorDrawStyles.Histogram;
                case SeriesType.Flag: return ChartIndicatorDrawStyles.Dot;
                case SeriesType.StackedArea: return ChartIndicatorDrawStyles.StackedBar;
                case SeriesType.Treemap: return ChartIndicatorDrawStyles.Dot;
                case SeriesType.Candle: return ChartIndicatorDrawStyles.Area;
                default: return ChartIndicatorDrawStyles.Line;
            }
        }

        private static ChartCandleDrawStyles ToCandleDrawStyle(SeriesType seriesType)
        {
            switch (seriesType)
            {
                case SeriesType.Line: return ChartCandleDrawStyles.LineClose;
                case SeriesType.Bar: return ChartCandleDrawStyles.Ohlc;
                case SeriesType.Candle: return ChartCandleDrawStyles.CandleStick;
                case SeriesType.StackedArea: return ChartCandleDrawStyles.Area;
                default: return ChartCandleDrawStyles.LineClose;
            }
        }

        private static decimal RoundLarge(decimal value)
        {
            if (value < 1000) return value;
            return Decimal.Round(value);
        }

        private static SecurityTypes ToSecurityTypes(this SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Option: return SecurityTypes.Option;
                case SecurityType.Commodity: return SecurityTypes.Commodity;
                case SecurityType.Forex: return SecurityTypes.Cfd;
                case SecurityType.Equity: return SecurityTypes.Stock;
                case SecurityType.Future: return SecurityTypes.Future;
                case SecurityType.Cfd: return SecurityTypes.Cfd;
                case SecurityType.Crypto: return SecurityTypes.CryptoCurrency;
                case SecurityType.FutureOption: return SecurityTypes.Future;
                case SecurityType.Index: return SecurityTypes.Index;
                case SecurityType.IndexOption: return SecurityTypes.Option;
                case SecurityType.CryptoFuture: return SecurityTypes.Future;
                default: return SecurityTypes.Stock;
            }
        }
    }
}
