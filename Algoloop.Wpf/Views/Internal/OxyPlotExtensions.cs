/*
 * Copyright 2023 Capnode AB
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

using OxyPlot.Series;
using OxyPlot;
using QuantConnect;
using System.Drawing;
using System;
using OxyPlot.Axes;

namespace Algoloop.Wpf.Views.Internal
{
    internal static class OxyPlotExtensions
    {
        private static Func<object, DataPoint> DataPointNumber = item => new DataPoint(((ChartPoint)item).x, (double)((ChartPoint)item).y);
        private static Func<object, DataPoint> DataPointTime = item => new DataPoint(DateTimeAxis.ToDouble(Time.UnixTimeStampToDateTime(((ChartPoint)item).x).ToLocalTime()), (double)((ChartPoint)item).y);
        private static Func<object, ScatterPoint> ScatterPointNumber = item => new ScatterPoint(((ChartPoint)item).x, (double)((ChartPoint)item).y);
        private static Func<object, ScatterPoint> ScatterPointTime = item => new ScatterPoint(DateTimeAxis.ToDouble(Time.UnixTimeStampToDateTime(((ChartPoint)item).x).ToLocalTime()), (double)((ChartPoint)item).y);


        public static ItemsSeries CreateSeries(this BaseSeries series, bool isTimeSeries)
        {
            string title = string.IsNullOrEmpty(series.Unit) ? series.Name : $"{series.Name} ({series.Unit})";
            switch (series.SeriesType)
            {
                case SeriesType.Line:
                case SeriesType.Treemap:
                    return new LineSeries
                    {
                        Title = title,
                        MarkerType = ToMarkerType(series),
                        Color = ToColor(series),
                        LineStyle = LineStyle.Solid,
                        Mapping = isTimeSeries ? DataPointTime : DataPointNumber,
                        ItemsSource = series.Values,
                    };
                case SeriesType.Candle:
                    return new AreaSeries
                    {
                        Title = title,
                        MarkerType = ToMarkerType(series),
                        Color = ToColor(series),
                        LineStyle = LineStyle.Solid,
                        Mapping = isTimeSeries ? DataPointTime : DataPointNumber,
                        ItemsSource = series.Values,
                    };
                case SeriesType.Pie:
                    return new PieSeries
                    {
                        Title = title,
                        ItemsSource = series.Values,
                    };
                case SeriesType.Scatter:
                    return new ScatterSeries
                    {
                        Title = title,
                        MarkerType = ToMarkerType(series, MarkerType.Circle),
                        MarkerFill = ToColor(series),
                        Mapping = isTimeSeries ? ScatterPointTime : ScatterPointNumber,
                        ItemsSource = series.Values,
                    };
                case SeriesType.Bar:
                    return new LinearBarSeries
                    {
                        Title = title,
                        FillColor = ToColor(series),
                        StrokeThickness = 1,
                        StrokeColor = OxyColor.FromArgb(255, 76, 175, 80),
                        NegativeFillColor = OxyColor.FromArgb(69, 191, 54, 12),
                        NegativeStrokeColor = OxyColor.FromArgb(255, 191, 54, 12),
                        Mapping = isTimeSeries ? DataPointTime : DataPointNumber,
                        ItemsSource = series.Values,
                    };
                case SeriesType.Flag:
                case SeriesType.StackedArea:
                default: return null;
            }
        }

        private static OxyColor ToColor(BaseSeries baseSeries)
        {
            if (baseSeries is QuantConnect.Series series)
            {
                Color color = series.Color;
                if (color.IsEmpty)
                {
                    return OxyColors.Automatic;
                }

                return OxyColor.FromArgb(color.A, color.R, color.G, color.B);
            }

            return OxyColors.Automatic;
        }

        private static MarkerType ToMarkerType(BaseSeries baseSeries, MarkerType none = MarkerType.None)
        {
            if (baseSeries is QuantConnect.Series series)
            {
                switch (series.ScatterMarkerSymbol)
                {
                    case ScatterMarkerSymbol.Square: return MarkerType.Square;
                    case ScatterMarkerSymbol.Circle: return MarkerType.Circle;
                    case ScatterMarkerSymbol.Diamond: return MarkerType.Diamond;
                    case ScatterMarkerSymbol.Triangle: return MarkerType.Triangle;
                    case ScatterMarkerSymbol.TriangleDown: return MarkerType.Triangle;
                    case ScatterMarkerSymbol.None:
                    default: return none;
                }
            }

            return none;
        }
    }
}
