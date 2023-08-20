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

using Algoloop.ViewModel;
using Algoloop.Wpf.Internal;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using QuantConnect;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Algoloop.Wpf
{
    /// <summary>
    /// Pan                 Right mouse button
    /// Zoom                Mouse wheel
    /// Zoom by rectangle   Ctrl+Right mouse button, Middle mouse button
    /// Reset               Ctrl+Right mouse button double-click, Middle mouse button double-click
    /// Show ‘tracker’      Left mouse button
    /// Reset axes          ‘A’, Home
    /// Copy code           Ctrl+Alt+C
    /// Copy properties     Ctrl+Alt+R
    /// 
    /// You can zoom/pan/reset a single axis by positioning the mouse cursor over the axis before starting the zoom/pan.
    /// </summary>
    public partial class PlotView : UserControl
    {
        private const string Separator = ";";
        private const double AssumeTime = 1E8;

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(ObservableCollection<IChartViewModel>),
            typeof(PlotView),
            new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));
        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
            "Settings",
            typeof(string),
            typeof(PlotView),
            new FrameworkPropertyMetadata { BindsTwoWayByDefault = true, DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

        private bool _isLoaded = false;

        public PlotView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            _chart.Name = "Chart";
            _chart.Drop += OnDrop;
            _chart.ItemsSource = Models;
        }

        public ObservableCollection<PlotModel> Models { get; } = new();
        
        public ObservableCollection<IChartViewModel> ItemsSource
        {
            get => (ObservableCollection<IChartViewModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string Settings
        {
            get => (string)GetValue(SettingsProperty);
            set => SetValue(SettingsProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PlotView chart = d as PlotView;
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
            chart.ItemsSourceChanged(charts);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is not Collection<IChartViewModel> collection) return;
            List<IChartViewModel> charts = collection.ToList();
            ItemsSourceChanged(charts);
        }

        private void ItemsSourceChanged(IEnumerable<IChartViewModel> charts)
        {
            _combobox.Items.Clear();
            if (charts == null) return;
            foreach (IChartViewModel chart in charts)
            {
                _combobox.Items.Add(new ChartItemViewModel(chart));
            }

            _combobox.SelectedIndex = 0;
            _combobox.Visibility = _combobox.Items.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            if (_isLoaded)
            {
                SetVisibleCharts();
                RedrawCharts();
            }
        }

        private void Combobox_DropDownClosed(object sender, EventArgs e)
        {
            var settings = new List<string>();
            foreach (ChartItemViewModel item in _combobox.Items)
            {
                if (item.IsVisible)
                {
                    settings.Add(item.Title);
                }
            }

            Settings = string.Join(Separator, settings);
            RedrawCharts();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded) return;
            Window window = Window.GetWindow(this);
            window.Closing += OnUnloaded;
            _isLoaded = true;
            SetVisibleCharts();
            RedrawCharts();
        }

        private void OnUnloaded(object sender, CancelEventArgs e)
        {
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(SymbolViewModel)) is not SymbolViewModel reference) return;
            if (_combobox.Items.Count < 1) return;
            if (_combobox.Items[0] is not SymbolChartViewModel target) return;
            target.Symbol.AddReferenceSymbol(reference);
            e.Handled = true;
        }

        private void SetVisibleCharts()
        {
            bool visible = string.IsNullOrEmpty(Settings); // Make first visible if no settings
            string[] activeCharts = Settings?.Split(Separator);
            foreach (ChartItemViewModel item in _combobox.Items)
            {
                item.IsVisible = visible || item.Chart.IsVisible || activeCharts.Contains(item.Chart.Title);
                visible = false; 
            }
        }

        private void RedrawCharts()
        {
            Models.Clear();
            foreach (ChartItemViewModel item in _combobox.Items)
            {
                if (!item.IsVisible) continue;
                if (item.Chart is not ChartViewModel chart) continue;
                var model = new PlotModel { Title = chart.Title };

                // Series
                bool isTime = false;
                foreach (BaseSeries baseSeries in chart.Chart.Series.Values)
                {
                    if (baseSeries.Values.Count == 0) continue;
                    ISeriesPoint first = baseSeries.Values[0];
                    if (first is not ChartPoint point) continue;
                    if (!isTime && point.x > AssumeTime)
                    {
                        isTime = true;
                        var xAxis = new DateTimeAxis();
                        model.Axes.Add(xAxis);
                    }

                    ItemsSeries series = baseSeries.CreateSeries(isTime);
                    if (series == null) continue;
                    model.Series.Add(series);
                }

                // Legend
                var legend = new Legend
                {
                    LegendBackground = OxyColor.FromArgb(200, 255, 255, 255),
                    LegendBorder = OxyColors.Black,
                    LegendPlacement = LegendPlacement.Inside,
                    LegendPosition = LegendPosition.TopLeft,
                };   
                model.Legends.Add(legend);

                Models.Add(model);
            }
        }
    }
}
