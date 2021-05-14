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
using AmCharts.Windows.Stock;
using AmCharts.Windows.Stock.Data;

namespace AmCharts
{
    public partial class AmChart : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = 
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<ChartViewModel>),
            typeof(AmChart), new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));

        private readonly List<Model> _models = new List<Model>();

        public AmChart()
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
            AmChart amChart = d as AmChart;
            Debug.Assert(amChart != null);

            if (e.OldValue != null)
            {
                // Unsubscribe from CollectionChanged on the old collection
                var coll = e.OldValue as INotifyCollectionChanged;
                coll.CollectionChanged -= amChart.OnCollectionChanged;
            }

            if (e.NewValue != null)
            {
                // Subscribe to CollectionChanged on the new collection
                var coll = e.NewValue as ObservableCollection<ChartViewModel>;
                coll.CollectionChanged += amChart.OnCollectionChanged;
            }

            var charts = e.NewValue as IEnumerable<ChartViewModel>;
            amChart.OnItemsSourceChanged(charts);
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
                var model = new Model(chart, selected || IsDefaultSelected(chart.DataSet.Title));
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
            stockChart.DataSets.Clear();
            Debug.Assert(stockChart.Charts.Count == 1);
            stockChart.Charts[0].Graphs.Clear();

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
            DataSet dataset = model.Chart.DataSet;
            stockChart.DataSets.Add(dataset);

            var graph = new Graph
            {
                GraphType = model.Chart.GraphType,
                Brush = model.Chart.Color,
                BulletType = GraphBulletType.RoundOutline,
                CursorBrush = model.Chart.Color,
                CursorSize = 6,
                DataField = DataItemField.Value,
                ShowLegendKey = true,
                ShowLegendTitle = true,
                LegendItemType = AmCharts.Windows.Stock.Primitives.LegendItemType.Value,
                LegendValueLabelText = "",
                LegendPeriodItemType = AmCharts.Windows.Stock.Primitives.LegendItemType.Value,
                LegendValueFormatString = "0.0000",
                PeriodValue = PeriodValue.Last,
                DataSet = dataset,
                Visibility = Visibility.Visible
            };

            stockChart.Charts[0].Graphs.Add(graph);
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
            Title = chart.DataSet.Title;
            IsSelected = selected;
        }

        public ChartViewModel Chart { get; }
        public string Title { get; }
        public bool IsSelected { get; set; }
    }
}
