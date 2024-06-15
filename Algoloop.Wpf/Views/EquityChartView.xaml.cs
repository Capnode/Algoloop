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

using Algoloop.Wpf.ViewModels;
using Algoloop.Wpf.ViewModels.Views.Internal.Lean;
using Algoloop.Wpf.Views.Internal;
using QuantConnect;
using StockSharp.Charting;
using StockSharp.Xaml.Charting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Algoloop.Wpf.Views
{
    public partial class EquityChartView : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = 
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<IChartViewModel>),
            typeof(EquityChartView), new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));
        private bool _isLoaded = false;

        public EquityChartView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public ObservableCollection<IChartViewModel> ItemsSource
        {
            get => (ObservableCollection<IChartViewModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            SafeRedrawCharts();
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
                SafeRedrawCharts();
            }
        }

        private void Combobox_DropDownClosed(object sender, EventArgs e)
        {
            SafeRedrawCharts();
        }

        private void SafeRedrawCharts()
        {
            try
            {
                RedrawCharts();
            }
            catch (Exception ex)
            {
                App.LogError(ex);
            }
        }


        private void RedrawCharts()
        {
            _chart.Clear();

            // Collect time-value points of all curves
            Dictionary<IChartBandElement, decimal> curves = new();
            Dictionary<DateTimeOffset, List<Tuple<IChartBandElement, decimal>>> points = new ();
            foreach (IChartViewModel iChart in _combobox.Items)
            {
                if (iChart is not ChartViewModel chart) continue;
                foreach (BaseSeries series in chart.Chart.Series.Values)
                {
                    IChartBandElement curveElement = _chart.CreateCurve(series.Name, System.Windows.Media.Colors.Black, ChartIndicatorDrawStyles.Line);
                    foreach (EquityData equityData in series.ToEquityData())
                    {
                        decimal value = equityData.Value;
                        if (!curves.ContainsKey(curveElement))
                        {
                            curves.Add(curveElement, value);
                        }

                        DateTimeOffset time = equityData.Time.Date;
                        if (!points.TryGetValue(time, out List<Tuple<IChartBandElement, decimal>> list))
                        {
                            list = new List<Tuple<IChartBandElement, decimal>>();
                            points.Add(time, list);
                        }
                        list.Add(new Tuple<IChartBandElement, decimal>(curveElement, value));
                    }
                }
            }

            // Draw all curves in time order, moment by moment
            foreach (KeyValuePair<DateTimeOffset, List<Tuple<IChartBandElement, decimal>>> moment in points.OrderBy(m => m.Key))
            {
                DateTimeOffset time = moment.Key;
                ChartDrawData chartData = new ();
                IChartDrawData.IChartDrawDataItem chartGroup = chartData.Group(time);
                foreach (KeyValuePair<IChartBandElement, decimal> curve in curves)
                {
                    IChartBandElement chart = curve.Key;
                    decimal value = curve.Value;

                    // Use actual point it available
                    Tuple<IChartBandElement, decimal> pair = moment.Value.Find(m => m.Item1.Equals(curve.Key));
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
