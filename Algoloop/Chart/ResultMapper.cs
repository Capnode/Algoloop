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
using QuantConnect;

namespace Algoloop.Charts
{
    // For many elements we use custom objects in this tool.
    public static class ResultMapper
    {
        public static Dictionary<string, ChartDefinition> MapToChartDefinitionDictionary(this IDictionary<string, Chart> sourceDictionary)
        {
            return sourceDictionary.ToDictionary(entry => entry.Key, entry => MapToChartDefinition(entry.Value));
        }

        public static Dictionary<string, Chart> MapToChartDictionary(this IDictionary<string, ChartDefinition> sourceDictionary)
        {
            return sourceDictionary.ToDictionary(entry => entry.Key, entry => MapToChart(entry.Value));
        }

        private static InstantChartPoint MapToTimeStampChartPoint(this ChartPoint point)
        {
            DateTime epoc = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            var instantPoint = new InstantChartPoint
            {
                X = epoc.AddSeconds(point.x),
                Y = point.y
            };

            return instantPoint;
        }

        private static ChartPoint MapToChartPoint(this InstantChartPoint point)
        {
            return new ChartPoint(point.X, point.Y);
        }

        private static ChartDefinition MapToChartDefinition(this Chart sourceChart)
        {
            return new ChartDefinition
            {
                Name = sourceChart.Name,
                Series = sourceChart.Series.MapToSeriesDefinitionDictionary()                
            };
        }

        private static Chart MapToChart(this ChartDefinition sourceChart)
        {
            return new Chart
            {
                Name = sourceChart.Name,
                Series = sourceChart.Series.MapToSeriesDictionary()
            };
        }

        private static Dictionary<string, SeriesDefinition> MapToSeriesDefinitionDictionary(this IDictionary<string, Series> sourceSeries)
        {
            return sourceSeries.ToDictionary(entry => entry.Key, entry => entry.Value.MapToSeriesDefinition());
        }

        private static Dictionary<string, Series> MapToSeriesDictionary(this IDictionary<string, SeriesDefinition> sourceSeries)
        {
            return sourceSeries.ToDictionary(entry => entry.Key, entry => entry.Value.MapToSeries());
        }

        private static ScatterMarkerSymbol MapToScatterMarkerSymbol(this QuantConnect.ScatterMarkerSymbol symbol)
        {
            switch (symbol)
            {
                case QuantConnect.ScatterMarkerSymbol.None:
                    return ScatterMarkerSymbol.None;
                    
                case QuantConnect.ScatterMarkerSymbol.Circle:
                    return ScatterMarkerSymbol.Circle;
                    
                case QuantConnect.ScatterMarkerSymbol.Square:
                    return ScatterMarkerSymbol.Square;
                    
                case QuantConnect.ScatterMarkerSymbol.Diamond:
                    return ScatterMarkerSymbol.Diamond;
                    
                case QuantConnect.ScatterMarkerSymbol.Triangle:
                    return ScatterMarkerSymbol.Triangle;

                case QuantConnect.ScatterMarkerSymbol.TriangleDown:
                    return ScatterMarkerSymbol.TriangleDown;

                default:
                    throw new NotSupportedException($"ScatterMarkerSymbol {symbol} is not supported.");
            }
        }

        private static QuantConnect.ScatterMarkerSymbol MapToQuantConnectScatterMarkerSymbol(this ScatterMarkerSymbol symbol)
        {
            switch (symbol)
            {
                case ScatterMarkerSymbol.None:
                    return QuantConnect.ScatterMarkerSymbol.None;

                case ScatterMarkerSymbol.Circle:
                    return QuantConnect.ScatterMarkerSymbol.Circle;

                case ScatterMarkerSymbol.Square:
                    return QuantConnect.ScatterMarkerSymbol.Square;

                case ScatterMarkerSymbol.Diamond:
                    return QuantConnect.ScatterMarkerSymbol.Diamond;

                case ScatterMarkerSymbol.Triangle:
                    return QuantConnect.ScatterMarkerSymbol.Triangle;

                case ScatterMarkerSymbol.TriangleDown:
                    return QuantConnect.ScatterMarkerSymbol.TriangleDown;

                default:
                    throw new NotSupportedException($"ScatterMarkerSymbol {symbol} is not supported.");
            }
        }

        private static SeriesType MapToSeriesType(this QuantConnect.SeriesType seriesType)
        {
            switch (seriesType)
            {
                case QuantConnect.SeriesType.Line:
                    return SeriesType.Line;
                    
                case QuantConnect.SeriesType.Scatter:
                    return SeriesType.Scatter;

                case QuantConnect.SeriesType.Candle:
                    return SeriesType.Candle;

                case QuantConnect.SeriesType.Bar:
                    return SeriesType.Bar;
                    
                default:
                    throw new NotSupportedException($"SeriesType {seriesType} is not supported.");
            }
        }

        private static QuantConnect.SeriesType MapToQuantConnectSeriesType(this SeriesType seriesType)
        {
            switch (seriesType)
            {
                case SeriesType.Line:
                    return QuantConnect.SeriesType.Line;

                case SeriesType.Scatter:
                    return QuantConnect.SeriesType.Scatter;

                case SeriesType.Candle:
                    return QuantConnect.SeriesType.Candle;

                case SeriesType.Bar:
                    return QuantConnect.SeriesType.Bar;

                default:
                    throw new NotSupportedException($"SeriesType {seriesType} is not supported.");
            }
        }

        private static SeriesDefinition MapToSeriesDefinition(this Series sourceSeries)
        {
            return new SeriesDefinition
            {
                Color = Color.FromArgb(sourceSeries.Color.A, sourceSeries.Color.R, sourceSeries.Color.G,sourceSeries.Color.B),
                Index = sourceSeries.Index,
                Name = sourceSeries.Name,
                ScatterMarkerSymbol = sourceSeries.ScatterMarkerSymbol.MapToScatterMarkerSymbol(),
                SeriesType = sourceSeries.SeriesType.MapToSeriesType(),
                Unit = sourceSeries.Unit,
                Values = sourceSeries.Values.Select(v => v.MapToTimeStampChartPoint()).ToList()
            };
        }

        private static Series MapToSeries(this SeriesDefinition sourceSeries)
        {
            return new Series
            {
                Color = System.Drawing.Color.FromArgb(sourceSeries.Color.A, sourceSeries.Color.R, sourceSeries.Color.G, sourceSeries.Color.B),
                Index = sourceSeries.Index,
                Name = sourceSeries.Name,
                ScatterMarkerSymbol = sourceSeries.ScatterMarkerSymbol.MapToQuantConnectScatterMarkerSymbol(),
                SeriesType = sourceSeries.SeriesType.MapToQuantConnectSeriesType(),
                Unit = sourceSeries.Unit,
                Values = sourceSeries.Values.Select(v => v.MapToChartPoint()).ToList()
            };
        }
    }
}