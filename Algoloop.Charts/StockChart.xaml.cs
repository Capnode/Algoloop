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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Algoloop.Charts
{
    public partial class StockChart : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = 
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<ChartViewModel>),
            typeof(StockChart), new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));

        private readonly List<Model> _models = new List<Model>();

        public StockChart()
        {
            InitializeComponent();
        }

        public ObservableCollection<ChartViewModel> ItemsSource
        {
            get => (ObservableCollection<ChartViewModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StockChart chart = d as StockChart;
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
                var coll = e.NewValue as ObservableCollection<ChartViewModel>;
                coll.CollectionChanged += chart.OnCollectionChanged;
            }

            var charts = e.NewValue as IEnumerable<ChartViewModel>;
            chart.OnItemsSourceChanged(charts);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            List<ChartViewModel> charts = e.NewItems?.Cast<ChartViewModel>().ToList();
            OnItemsSourceChanged(charts);
        }

        private void OnItemsSourceChanged(IEnumerable<ChartViewModel> charts)
        {
            // Clear charts
            _combobox.Items.Clear();
            _models.Clear();
            if (charts == null)
                return;

            bool selected = true;
            foreach (ChartViewModel chart in charts)
            {
                var model = new Model(chart, selected || IsDefaultSelected(chart.Title));
                _combobox.Items.Add(model);
                selected = false;
            }

            _combobox.SelectedIndex = 0;
            _combobox.Visibility = _combobox.Items.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

            RedrawCharts();
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

        private void RedrawCharts()
        {
            foreach (var item in _combobox.Items)
            {
                if (item is Model model && model.IsSelected)
                {
                    RedrawChart(model);
                }
            }
        }

        private void RedrawChart(Model model)
        {
        }

        private void Combobox_DropDownClosed(object sender, EventArgs e)
        {
            RedrawCharts();
        }
    }

    class Model
    {
        public Model(ChartViewModel chart, bool selected)
        {
            Chart = chart;
            Title = chart.Title;
            IsSelected = selected;
        }

        public ChartViewModel Chart { get; }
        public string Title { get; }
        public bool IsSelected { get; set; }
    }
}
