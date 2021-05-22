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
using StockSharp.Xaml.Charting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Algoloop.Wpf.View
{
    public partial class EquityChartView : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = 
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<EquityChartViewModel>),
            typeof(EquityChartView), new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));
        private bool _isLoaded = false;

        public EquityChartView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public ObservableCollection<EquityChartViewModel> ItemsSource
        {
            get => (ObservableCollection<EquityChartViewModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            RedrawCharts();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EquityChartView chart = d as EquityChartView;

            if (e.OldValue != null)
            {
                // Unsubscribe from CollectionChanged on the old collection
                var coll = e.OldValue as INotifyCollectionChanged;
                coll.CollectionChanged -= chart.OnCollectionChanged;
            }

            if (e.NewValue != null)
            {
                // Subscribe to CollectionChanged on the new collection
                var coll = e.NewValue as ObservableCollection<EquityChartViewModel>;
                coll.CollectionChanged += chart.OnCollectionChanged;
            }

            var charts = e.NewValue as IEnumerable<EquityChartViewModel>;
            chart.OnItemsSourceChanged(charts);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            List<EquityChartViewModel> charts = e.NewItems?.Cast<EquityChartViewModel>().ToList();
            OnItemsSourceChanged(charts);
        }

        private void OnItemsSourceChanged(IEnumerable<EquityChartViewModel> charts)
        {
            // Clear charts
            _combobox.Items.Clear();
            if (charts == null) return;
            bool selected = true;
            foreach (EquityChartViewModel chart in charts)
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
            _chart.Clear();

            // Collect time-value points of all curves
            Dictionary<ChartBandElement, decimal> curves = new();
            Dictionary<DateTimeOffset, List<Tuple<ChartBandElement, decimal>>> points = new ();
            foreach (object item in _combobox.Items)
            {
                if (item is EquityChartViewModel model && model.IsSelected)
                {
                    ChartBandElement curveElement = _chart.CreateCurve(model.Title, model.Color, ChartIndicatorDrawStyles.Line);
                    foreach (EquityData equityData in model.Series)
                    {
                        decimal value = equityData.Value;
                        if (!curves.ContainsKey(curveElement))
                        {
                            curves.Add(curveElement, value);
                        }

                        DateTimeOffset time = equityData.Time.Date;
                        if (!points.TryGetValue(time, out List<Tuple<ChartBandElement, decimal>> list))
                        {
                            list = new List<Tuple<ChartBandElement, decimal>>();
                            points.Add(time, list);
                        }
                        list.Add(new Tuple<ChartBandElement, decimal>(curveElement, value));
                    }
                }
            }

            // Draw all curves in time order, moment by moment
            foreach (KeyValuePair<DateTimeOffset, List<Tuple<ChartBandElement, decimal>>> moment in points.OrderBy(m => m.Key))
            {
                DateTimeOffset time = moment.Key;
                ChartDrawData chartData = new();
                ChartDrawData.ChartDrawDataItem chartGroup = chartData.Group(time);
                foreach (KeyValuePair<ChartBandElement, decimal> curve in curves)
                {
                    ChartBandElement chart = curve.Key;
                    decimal value = curve.Value;

                    // Use actual point it available
                    Tuple<ChartBandElement, decimal> pair = moment.Value.Find(m => m.Item1.Equals(curve.Key));
                    if (pair != default)
                    {
                        value = pair.Item2;
                        curves[chart] = value;
                    }
                    chartGroup.Add(chart, value);
                }

                _chart.Draw(chartData);
            }
        }
    }
}
