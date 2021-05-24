/*
 * Copyright 2021 Capnode AB
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

using Algoloop.Wpf.ViewModel;
using MoreLinq;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Xaml.Charting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Algoloop.Wpf.View
{
    public partial class StockChartView : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = 
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<IChartViewModel>),
            typeof(StockChartView), new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));
        private bool _isLoaded = false;

        public StockChartView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            _chart.IsInteracted = true;
            _chart.IsAutoRange = true;
            _chart.IsAutoScroll = true;
            _chart.ShowOverview = true;
            _chart.ShowPerfStats = false;
            _chart.AllowAddCandles = false;
            _chart.AllowAddIndicators = true;
            _chart.AllowAddOrders = false;
            _chart.AllowAddOwnTrades = false;
            _chart.AllowAddAxis = false;
            _chart.AllowAddArea = false;

            _chart.SubscribeIndicatorElement += OnSubscribeIndicatorElement;
            _chart.UnSubscribeElement += OnUnSubscribeElement;
        }

        public ObservableCollection<IChartViewModel> ItemsSource
        {
            get => (ObservableCollection<IChartViewModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            _chart.FillIndicators();
            RedrawCharts();
        }

        private void OnUnSubscribeElement(IChartElement obj)
        {
        }

        private void OnSubscribeIndicatorElement(ChartIndicatorElement arg1, CandleSeries arg2, StockSharp.Algo.Indicators.IIndicator arg3)
        {
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StockChartView chart = d as StockChartView;
            Debug.Assert(chart != null);

            if (e.OldValue != null)
            {
                // Unsubscribe from CollectionChanged on the old collection
                var coll = e.OldValue as INotifyCollectionChanged;
                coll.CollectionChanged -= chart.OnCollectionChanged;
            }

            if (e.NewValue != null)
            {
                // Subscribe to CollectionChanged on the new collection
                var coll = e.NewValue as ObservableCollection<IChartViewModel>;
                coll.CollectionChanged += chart.OnCollectionChanged;
            }

            var charts = e.NewValue as IEnumerable<IChartViewModel>;
            chart.OnItemsSourceChanged(charts);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            List<IChartViewModel> charts = e.NewItems?.Cast<IChartViewModel>().ToList();
            OnItemsSourceChanged(charts);
        }

        private void OnItemsSourceChanged(IEnumerable<IChartViewModel> charts)
        {
            // Clear charts
            _combobox.Items.Clear();
            if (charts == null) return;
            bool selected = true;
            foreach (IChartViewModel chart in charts)
            {
                chart.IsSelected = selected;
                _combobox.Items.Add(chart);
                selected = false;
            }

            _combobox.SelectedIndex = 0;
            _combobox.Visibility = _combobox.Items.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            if (_isLoaded)
            {
                RedrawCharts();
            }
        }

        private void Combobox_DropDownClosed(object sender, EventArgs e)
        {
            RedrawCharts();
        }

        private void RedrawCharts()
        {
            _chart.IsAutoRange = true;
            _chart.ClearAreas();
            ChartArea candlesArea = new ();
            _chart.AddArea(candlesArea);
            foreach (IChartViewModel chart in _combobox.Items)
            {
                if (!chart.IsSelected) continue;
                if (chart is StockChartViewModel stockChart)
                {
                    RedrawChart(candlesArea, stockChart);
                }
                else if (chart is EquityChartViewModel)
                {
                    _chart.IsInteracted = false;
                    RedrawEquityCharts(candlesArea);
                    break;
                }
            }

            _chart.IsAutoRange = false; // Allow user to adjust range
        }

        private void RedrawChart(ChartArea candlesArea, StockChartViewModel model)
        {
            Candle first = model.Candles.FirstOrDefault();
            if (first == default) return;
            CandleSeries series;
            if (first is TimeFrameCandle tf)
            {
                series = new CandleSeries(typeof(TimeFrameCandle), tf.Security, tf.TimeFrame);
            }
            else
            {
                throw new NotImplementedException("Candle subtype not implemented");
            }

            var candleElement = new ChartCandleElement
            {
                UpBorderColor = model.Color,
                DownBorderColor = model.Color
            };
            _chart.AddElement(candlesArea, candleElement, series);
            var chartData = new ChartDrawData();
            foreach (Candle candle in model.Candles)
            {
                ChartDrawData.ChartDrawDataItem chartGroup = chartData.Group(candle.OpenTime);
                chartGroup.Add(candleElement, candle);
            }
            _chart.Draw(chartData);
        }

        private void RedrawEquityCharts(ChartArea candlesArea)
        {
            // Collect time-value points of all curves
            Dictionary<ChartCandleElement, decimal> curves = new();
            Dictionary<DateTimeOffset, List<Tuple<ChartCandleElement, decimal>>> points = new();
            foreach (object item in _combobox.Items)
            {
                if (item is EquityChartViewModel model && model.IsSelected)
                {
                    Security security = new () { Id = model.Title };
                    CandleSeries series = new (typeof(TimeFrameCandle), security, TimeSpan.FromDays(1));
                    ChartCandleElement curveElement = new()
                    {
                        DrawStyle = ChartCandleDrawStyles.Area,
                        LineColor = model.Color,
                        FontColor = model.Color,
                        AreaColor = Colors.LightGray
                    };
                    _chart.AddElement(candlesArea, curveElement, series);
                    foreach (EquityData equityData in model.Series)
                    {
                        decimal value = equityData.Value;
                        if (!curves.ContainsKey(curveElement))
                        {
                            curves.Add(curveElement, value);
                        }

                        DateTimeOffset time = equityData.Time.Date;
                        if (!points.TryGetValue(time, out List<Tuple<ChartCandleElement, decimal>> list))
                        {
                            list = new List<Tuple<ChartCandleElement, decimal>>();
                            points.Add(time, list);
                        }
                        list.Add(new Tuple<ChartCandleElement, decimal>(curveElement, value));
                    }
                }
            }

            // Draw all curves in time order, moment by moment
            foreach (KeyValuePair<DateTimeOffset, List<Tuple<ChartCandleElement, decimal>>> moment in points.OrderBy(m => m.Key))
            {
                DateTimeOffset time = moment.Key;
                ChartDrawData chartData = new();
                ChartDrawData.ChartDrawDataItem chartGroup = chartData.Group(time);
                foreach (KeyValuePair<ChartCandleElement, decimal> curve in curves)
                {
                    ChartCandleElement chart = curve.Key;
                    decimal value = curve.Value;

                    // Use actual point it available
                    Tuple<ChartCandleElement, decimal> pair = moment.Value.Find(m => m.Item1.Equals(curve.Key));
                    if (pair != default)
                    {
                        value = pair.Item2;
                        curves[chart] = value;
                    }
                    Security security = new() { Id = "Id" };
                    TimeFrameCandle candle = new()
                    {
                        Security = security,
                        TimeFrame = TimeSpan.FromDays(1),
                        OpenTime = time,
                        HighTime = time,
                        LowTime = time,
                        CloseTime = time,
                        OpenPrice = value,
                        HighPrice = value,
                        LowPrice = value,
                        ClosePrice = value,
                        OpenVolume = 0,
                        HighVolume = 0,
                        LowVolume = 0,
                        CloseVolume = 0,
                        BuildFrom = StockSharp.Messages.DataType.Ticks,
                        State = StockSharp.Messages.CandleStates.Finished
                    };
                    chartGroup.Add(chart, candle);
                }

                _chart.Draw(chartData);
            }
        }
    }
}
