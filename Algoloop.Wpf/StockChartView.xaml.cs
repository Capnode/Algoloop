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

using Algoloop.ViewModel;
using Ecng.Collections;
using Ecng.Serialization;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Charting;
using StockSharp.Configuration;
using StockSharp.Xaml.Charting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Algoloop.Wpf
{
    /// <summary>
    /// 
    /// StockShart doc: https://doc.stocksharp.com/topics/StockSharpAbout.html
    /// </summary>
    public partial class StockChartView : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(ObservableCollection<IChartViewModel>),
            typeof(StockChartView),
            new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));
        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
            "Settings",
            typeof(string),
            typeof(StockChartView),
            new FrameworkPropertyMetadata { BindsTwoWayByDefault = true, DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

        private bool _isLoaded = false;

        private readonly CachedSynchronizedDictionary<IChartIndicatorElement, IIndicator> _indicators = new();

        public StockChartView()
        {
            InitializeComponent();
            Loaded += OnLoaded;

            _chart.IsInteracted = true;
            _chart.IsAutoRange = true;
            _chart.IsAutoScroll = true;
            _chart.MinimumRange = int.MaxValue;
            _chart.ShowOverview = true;
            _chart.ShowPerfStats = false;
            _chart.AllowAddCandles = true;
            _chart.AllowAddIndicators = true;
            _chart.AllowAddOrders = false;
            _chart.AllowAddOwnTrades = false;
            _chart.AllowAddAxis = true;
            _chart.AllowAddArea = true;

            _chart.SubscribeCandleElement += OnSubscribeCandleElement;
            _chart.SubscribeIndicatorElement += OnSubscribeIndicatorElement;
            _chart.UnSubscribeElement += OnUnSubscribeElement;
            _chart.AnnotationCreated += OnAnnotationCreated;
            _chart.AnnotationModified += OnAnnotationModified;
            _chart.AnnotationDeleted += OnAnnotationDeleted;
            _chart.AnnotationSelected += OnAnnotationSelected;
        }

        public ObservableCollection<IChartViewModel> ItemsSource
        {
            get => (ObservableCollection<IChartViewModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string Settings
        {
            get => (string)base.GetValue(SettingsProperty);
            set => SetValue(SettingsProperty, value);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            Window window = Window.GetWindow(this);
            window.Closing += OnUnloaded;

            _chart.FillIndicators();
            byte[] bytes = Encoding.Default.GetBytes(Settings);
            SettingsStorage storage = bytes.Deserialize<SettingsStorage>();
            _chart.LoadIfNotNull(storage);
            RedrawCharts();
        }

        private void OnUnloaded(object sender, CancelEventArgs e)
        {
            var settingsStorage = new SettingsStorage();
            _chart.Save(settingsStorage);
            byte[] bytes = settingsStorage.Serialize();
            Settings = Encoding.Default.GetString(bytes);
        }

        private void OnSubscribeIndicatorElement(IChartIndicatorElement element, CandleSeries series, IIndicator indicator)
        {
            bool oldReset = _chart.DisableIndicatorReset;
            try
            {
                _chart.DisableIndicatorReset = true;
                indicator.Reset();
            }
            finally
            {
                _chart.DisableIndicatorReset = oldReset;
            }

            // Process Candles
            IChartDrawData chartData = _chart.CreateData();
            foreach (IChartViewModel chart in _combobox.Items)
            {
                if (!chart.IsVisible) continue;
                if (chart is not StockChartViewModel stockChart) continue;
                foreach (Candle candle in stockChart.Candles)
                {
                    chartData.Group(candle.OpenTime).Add(element, indicator.Process(candle));
                }
            }

            _chart.Reset(new[] { element });
            _chart.Draw(chartData);

            _indicators[element] = indicator;
        }

        private void OnUnSubscribeElement(IChartElement element)
        {
            if (element is IChartIndicatorElement indElem)
            {
                _indicators.Remove(indElem);
            }
        }

        private void OnAnnotationSelected(IChartAnnotation arg1, ChartDrawData.AnnotationData arg2)
        {
        }

        private void OnAnnotationDeleted(IChartAnnotation obj)
        {
        }

        private void OnAnnotationModified(IChartAnnotation arg1, ChartDrawData.AnnotationData arg2)
        {
        }

        private void OnAnnotationCreated(IChartAnnotation obj)
        {
        }

        private void OnSubscribeCandleElement(IChartCandleElement arg1, CandleSeries arg2)
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
            foreach (IChartViewModel chart in charts)
            {
                _combobox.Items.Add(chart);
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
            _chart.Reset(_chart.Elements);
            foreach (IChartArea area in _chart.Areas)
            {
                area.Elements.Clear();
                area.XAxises.RemoveRange(area.XAxises.Skip(1));
                area.YAxises.RemoveRange(area.YAxises.Skip(1));
            }

            // Collect time-value points of all Equity curves
            Dictionary<ChartLineElement, decimal> curves = new();
            Dictionary<DateTimeOffset, List<Tuple<ChartLineElement, decimal>>> points = new();
            foreach (IChartViewModel chart in _combobox.Items)
            {
                if (!chart.IsVisible) continue;
                IChartArea area;
                if (chart.SubChart < _chart.Areas.Count)
                {
                    area = _chart.Areas[chart.SubChart];
                }
                else
                {
                    area = new ChartArea();
                    _chart.AddArea(area);
                }

                if (chart is EquityChartViewModel model)
                {
                    ChartLineElement lineElement = new()
                    {
                        FullTitle = model.Title,
                        Style = model.Style,
                        Color = model.Color,
                        AntiAliasing = false,
                        IsLegend = true,
                        ShowAxisMarker = true
                    };
                    _chart.AddElement(area, lineElement);
                    foreach (EquityData equityData in model.Series)
                    {
                        decimal value = equityData.Value;
                        if (!curves.ContainsKey(lineElement))
                        {
                            curves.Add(lineElement, value);
                        }

                        DateTimeOffset time = equityData.Time.Date;
                        if (!points.TryGetValue(time, out List<Tuple<ChartLineElement, decimal>> list))
                        {
                            list = new List<Tuple<ChartLineElement, decimal>>();
                            points.Add(time, list);
                        }
                        list.Add(new Tuple<ChartLineElement, decimal>(lineElement, value));
                    }
                }
            }

            // Draw all equity curves in time order, moment by moment
            foreach (KeyValuePair<DateTimeOffset, List<Tuple<ChartLineElement, decimal>>> moment in points.OrderBy(m => m.Key))
            {
                DateTimeOffset time = moment.Key;
                ChartDrawData chartData = new();
                IChartDrawData.IChartDrawDataItem chartGroup = chartData.Group(time);
                foreach (KeyValuePair<ChartLineElement, decimal> curve in curves)
                {
                    ChartLineElement lineElement = curve.Key;
                    decimal value = curve.Value;

                    // Use actual point it available
                    Tuple<ChartLineElement, decimal> pair = moment.Value.Find(m => m.Item1.Equals(curve.Key));
                    if (pair != default)
                    {
                        value = pair.Item2;
                        curves[lineElement] = value;
                    }
                    chartGroup.Add(lineElement, value);
                }

                _chart.IsInteracted = false;
                _chart.Draw(chartData);
            }

            // Draw Candles
            foreach (IChartViewModel chart in _combobox.Items)
            {
                if (!chart.IsVisible) continue;
                if (chart.SubChart >= _chart.Areas.Count) continue;
                IChartArea area = _chart.Areas[chart.SubChart];
                if (chart is StockChartViewModel stockChart)
                {
                    RedrawChart(area, stockChart);
                }
            }

            _chart.IsAutoRange = false; // Allow user to adjust range
        }

        private void RedrawChart(IChartArea candlesArea, StockChartViewModel model)
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
                DrawStyle = model.Style,
                UpBorderColor = model.Color,
                DownBorderColor = model.Color
            };
            _chart.AddElement(candlesArea, candleElement, series);
            var chartData = new ChartDrawData();
            foreach (Candle candle in model.Candles)
            {
                IChartDrawData.IChartDrawDataItem chartGroup = chartData.Group(candle.OpenTime);
                chartGroup.Add(candleElement, candle);
            }
            _chart.Draw(chartData);
        }
    }
}
