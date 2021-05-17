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

using MoreLinq;
using StockSharp.Algo.Candles;
using StockSharp.Xaml.Charting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Algoloop.Wpf.View
{
    public partial class StockChartView : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = 
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<StockChartViewModel>),
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
            _chart.AllowAddCandles = false;
            _chart.AllowAddIndicators = true;
            _chart.AllowAddOrders = false;
            _chart.AllowAddOwnTrades = false;
            _chart.AllowAddAxis = false;
            _chart.AllowAddArea = false;
            _chart.SubscribeIndicatorElement += OnSubscribeIndicatorElement;
            _chart.UnSubscribeElement += OnUnSubscribeElement;
        }

        public ObservableCollection<StockChartViewModel> ItemsSource
        {
            get => (ObservableCollection<StockChartViewModel>)GetValue(ItemsSourceProperty);
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
                var coll = e.NewValue as ObservableCollection<StockChartViewModel>;
                coll.CollectionChanged += chart.OnCollectionChanged;
            }

            var charts = e.NewValue as IEnumerable<StockChartViewModel>;
            chart.OnItemsSourceChanged(charts);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            List<StockChartViewModel> charts = e.NewItems?.Cast<StockChartViewModel>().ToList();
            OnItemsSourceChanged(charts);
        }

        private void OnItemsSourceChanged(IEnumerable<StockChartViewModel> charts)
        {
            // Clear charts
            _combobox.Items.Clear();
            if (charts == null) return;
            bool selected = true;
            foreach (StockChartViewModel chart in charts)
            {
                chart.IsSelected = selected || IsDefaultSelected(chart.Title);
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

        private static bool IsDefaultSelected(string title)
        {
            return title switch
            {
                "Net profit" => true,
                "Equity" => true,
                _ => false,
            };
        }

        private void Combobox_DropDownClosed(object sender, EventArgs e)
        {
            RedrawCharts();
        }

        private void RedrawCharts()
        {
            _chart.IsAutoRange = true;
            _chart.ClearAreas();
            ChartArea candlesArea = new ChartArea();
            _chart.AddArea(candlesArea);
            foreach (object item in _combobox.Items)
            {
                if (item is StockChartViewModel chart && chart.IsSelected)
                {
                    RedrawChart(candlesArea, chart);
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
    }
}
