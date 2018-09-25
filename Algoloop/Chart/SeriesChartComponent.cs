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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Definitions.Series;
using LiveCharts.Wpf;
using QuantConnect;
using Series = LiveCharts.Wpf.Series;

namespace Algoloop.Charts
{
    /// <summary>
    /// Chart component responsible for working with series
    /// </summary>
    public class SeriesChartComponent
    {
        private Resolution _resolution;
        private readonly List<Color> _defaultColors = new List<Color>
        {
            Color.FromArgb(0, 0, 0, 0),
            Color.FromArgb(255, 0, 0, 0)
        };

        public SeriesChartComponent(Resolution resolution)
        {
            _resolution = resolution;
        }

        public static Resolution DetectResolution(SeriesDefinition series)
        {
            if (series.SeriesType == SeriesType.Candle)
            {
                // Candle data is supposed to be grouped.
                // Currently we group candle data by day.
                return Resolution.Daily;
            }

            var chartPoints = series.Values;

            // Check whether we have duplicates in day mode

            var dayDuplicates = chartPoints.GroupBy(cp => cp.X.Ticks / TimeSpan.TicksPerDay).Any(g => g.Count() > 1);
            if (!dayDuplicates)
                return Resolution.Daily;

            var hourDuplicates = chartPoints.GroupBy(cp => cp.X.Ticks / TimeSpan.TicksPerHour).Any(g => g.Count() > 1);
            if (!hourDuplicates)
                return Resolution.Hour;

            var minuteDuplicates = chartPoints.GroupBy(cp => cp.X.Ticks / TimeSpan.TicksPerMinute).Any(g => g.Count() > 1);
            if (!minuteDuplicates)
                return Resolution.Minute;

            var secondDuplicates = chartPoints.GroupBy(cp => cp.X.Ticks / TimeSpan.TicksPerSecond).Any(g => g.Count() > 1);
            if (!secondDuplicates)
                return Resolution.Second;

            return Resolution.Tick;
        }

        public Series BuildSeries(SeriesDefinition sourceSeries)
        {
            Series series;

            switch (sourceSeries.SeriesType)
            {
                case SeriesType.Line:
                    series = new LineSeries
                    {
                        Configuration = new InstantChartPointMapper(),
                        Fill = Brushes.Transparent
                    };
                    break;

                case SeriesType.Bar:
                    series = new ColumnSeries
                    {
                        Configuration = new InstantChartPointMapper(),
                    };
                    break;

                case SeriesType.Candle:
                    series = new CandleSeries
                    {
                        Configuration = new OhlcInstantChartPointMapper(),
                        IncreaseBrush = Brushes.Green,
                        DecreaseBrush = Brushes.Red,
                        Fill = Brushes.Transparent
                    };
                    break;

                case SeriesType.Scatter:
                    series = new ScatterSeries
                    {
                        Configuration = new InstantChartPointMapper(),
                        StrokeThickness = 1
                    };
                    break;

                default:
                    throw new NotSupportedException();
            }

            series.Title = sourceSeries.Name;
            series.PointGeometry = GetPointGeometry(sourceSeries.ScatterMarkerSymbol);

            // Check whether the series has a color configured
            if (_defaultColors.All(c => !sourceSeries.Color.Equals(c)))
            {
                // No default color present. use it for the stroke
                var brush = new SolidColorBrush(sourceSeries.Color);
                brush.Freeze();

                switch (sourceSeries.SeriesType)
                {
                    case SeriesType.Candle:
                        series.Fill = brush;
                        break;

                    default:
                        series.Stroke = brush;
                        break;
                }
            }

            return series;
        }

        public void UpdateSeries(ISeriesView targetSeries, SeriesDefinition sourceSeries)
        {
            // Detect the data resolution of the source series.
            // Use it as chart resolution if needed.
            var detectedResolution = DetectResolution(sourceSeries);
            if (_resolution > detectedResolution) _resolution = detectedResolution;           

            // QuantChart series are unix timestamp
            switch (sourceSeries.SeriesType)
            {
                case SeriesType.Scatter:
                case SeriesType.Bar:
                case SeriesType.Line:
                    var existingCommonValues = (ChartValues<InstantChartPoint>)(targetSeries.Values ?? (targetSeries.Values = new ChartValues<InstantChartPoint>()));
                    existingCommonValues.AddRange(sourceSeries.Values);
                    break;

                case SeriesType.Candle:
                    // Build daily candles
                    var existingCandleValues = (ChartValues<OhlcInstantChartPoint>)(targetSeries.Values ?? (targetSeries.Values = new ChartValues<OhlcInstantChartPoint>()));
                    var newValues = sourceSeries.Values.GroupBy(cp => cp.X.Ticks / TimeSpan.TicksPerDay).Select(
                        g =>
                        {
                            return new OhlcInstantChartPoint
                            {
                                X = g.First().X,
                                Open = (double)g.First().Y,
                                Close = (double)g.Last().Y,
                                Low = (double)g.Min(z => z.Y),
                                High = (double)g.Max(z => z.Y)
                            };
                        }).ToList();

                    // Update existing ohlc points.
                    UpdateExistingOhlcPoints(existingCandleValues, newValues, Resolution.Daily);

                    existingCandleValues.AddRange(newValues);
                    break;

                default:
                    throw new Exception($"SeriesType {sourceSeries.SeriesType} is not supported.");
            }
        }

        public void UpdateExistingOhlcPoints(IList<OhlcInstantChartPoint> existingPoints, IList<OhlcInstantChartPoint> updatedPoints, Resolution resolution)
        {
            // Check whether we are updating existing points
            if (existingPoints.Count <= 0) return;

            if (resolution != Resolution.Daily)
            {
                throw new ArgumentOutOfRangeException($"Resolution {resolution} is not supported. Only Day is supported.");
            }

            // Check whether we have new information for the last ohlc point
            var lastKnownDay = existingPoints.Last().X.Ticks / TimeSpan.TicksPerSecond * 60 * 24;
            while (updatedPoints.Any() && (updatedPoints.First().X.Ticks / TimeSpan.TicksPerSecond * 60 * 24 <= lastKnownDay)) // We assume we always show ohlc in day groups
            {
                // Update the last ohlc point with this inforrmation
                var refval = updatedPoints.First();

                // find the value matching this day
                var ohlcEquityChartValue = existingPoints.Last();

                // Update ohlc point with highest and lowest, and with the new closing price
                // Update the normal point with the new closing value
                ohlcEquityChartValue.High = Math.Max(refval.High, ohlcEquityChartValue.High);
                ohlcEquityChartValue.Low = Math.Min(refval.Low, ohlcEquityChartValue.Low);
                ohlcEquityChartValue.Close = refval.Close;

                // Remove this value, as it has been parsed into existing chart points
                updatedPoints.RemoveAt(0);
            }
        }

        private static Geometry GetPointGeometry(ScatterMarkerSymbol symbol)
        {
            switch (symbol)
            {
                case ScatterMarkerSymbol.None:
                    return DefaultGeometries.None;

                case ScatterMarkerSymbol.Circle:
                    return DefaultGeometries.Circle;

                case ScatterMarkerSymbol.Diamond:
                    return DefaultGeometries.Diamond;

                case ScatterMarkerSymbol.Square:
                    return DefaultGeometries.Square;

                case ScatterMarkerSymbol.Triangle:
                    return DefaultGeometries.Triangle;

                case ScatterMarkerSymbol.TriangleDown:
                    return Geometries.TriangleDown;

                default:
                    return DefaultGeometries.Circle;

            }
        }        
    }
}