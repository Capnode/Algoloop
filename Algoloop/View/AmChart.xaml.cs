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
using QuantConnect;
using QuantConnect.Data.Market;

namespace Algoloop.View
{
    public partial class AmChart : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = 
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<ChartViewModel>),
            typeof(AmChart), new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));

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

            var charts = e.NewValue as IReadOnlyList<ChartViewModel>;
            amChart.OnItemsSourceChanged(charts);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            List<ChartViewModel> charts = e.NewItems?.Cast<ChartViewModel>().ToList();
            OnItemsSourceChanged(charts);
        }

        private void OnItemsSourceChanged(IReadOnlyList<ChartViewModel> charts)
        {
            // Clear charts
            stockChart.DataSets.Clear();
            Debug.Assert(stockChart.Charts.Count == 1);
            stockChart.Charts[0].Graphs.Clear();
            if (charts == null)
                return;

            Visibility visibility = Visibility.Visible;
            foreach (ChartViewModel chart in charts)
            {
                DataSet dataset = null;
                QuantConnect.Data.BaseData item = chart.Data?.FirstOrDefault();
                if (item == null)
                {
                    dataset = ChartSeries(chart);
                }
                else if (item.GetType() == typeof(QuoteBar))
                {
                    dataset = QuoteBarData(chart);
                }
                else if (item.GetType() == typeof(TradeBar))
                {
                    dataset = QuoteBarData(chart);
                }
                else if (item.GetType() == typeof(Tick))
                {
                    dataset = TickData(chart);
                }

                stockChart.DataSets.Add(dataset);

                var graph = new Graph
                {
                    GraphType = ToGraphType(chart.Series.SeriesType),
                    Brush = ToMediaBrush(chart.Series.Color),
                    BulletType = GraphBulletType.RoundOutline,
                    CursorBrush = ToMediaBrush(chart.Series.Color),
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
                    Visibility = visibility
                };

                stockChart.Charts[0].Graphs.Add(graph);
                visibility = Visibility.Hidden;
            }
        }

        private static DataSet ChartSeries(ChartViewModel chart)
        {
            DataSet dataset;
            int ix = 0;
            int size = chart.Series.Values.Count;
            KeyValuePair<DateTime, double>[] timeseries = new KeyValuePair<DateTime, double>[size];
            foreach (ChartPoint point in chart.Series.Values)
            {
                DateTime time = Time.UnixTimeStampToDateTime(point.x);
                timeseries[ix++] = new KeyValuePair<DateTime, double>(time, (double)point.y);
            }

            dataset = new DataSet()
            {
                ID = "id",
                Title = chart.Title,
                ShortTitle = chart.Title,
                DateMemberPath = "Key",
                OpenMemberPath = "Value",
                HighMemberPath = "Value",
                LowMemberPath = "Value",
                CloseMemberPath = "Value",
                ValueMemberPath = "Value",
                IsSelectedForComparison = false,
                ItemsSource = timeseries,
                IsVisibleInCompareDataSetSelector = false,
                IsVisibleInMainDataSetSelector = false,
                StartDate = timeseries.First().Key,
                EndDate = timeseries.Last().Key
            };
            return dataset;
        }

        private static DataSet QuoteBarData(ChartViewModel chart)
        {
            return new DataSet()
            {
                ID = "id",
                Title = chart.Title,
                ShortTitle = chart.Title,
                DateMemberPath = "Time",
                OpenMemberPath = "Open",
                HighMemberPath = "High",
                LowMemberPath = "Low",
                CloseMemberPath = "Close",
                ValueMemberPath = "Value",
                IsSelectedForComparison = false,
                ItemsSource = chart.Data,
                IsVisibleInCompareDataSetSelector = false,
                IsVisibleInMainDataSetSelector = false,
                StartDate = chart.Data.First().Time,
                EndDate = chart.Data.Last().Time,
            };
        }

        private DataSet TickData(ChartViewModel chart)
        {
            return new DataSet()
            {
                ID = "id",
                Title = chart.Title,
                ShortTitle = chart.Title,
                DateMemberPath = "Time",
                OpenMemberPath = "Value",
                HighMemberPath = "Value",
                LowMemberPath = "Value",
                CloseMemberPath = "Value",
                ValueMemberPath = "Value",
                IsSelectedForComparison = false,
                ItemsSource = chart.Data,
                IsVisibleInCompareDataSetSelector = false,
                IsVisibleInMainDataSetSelector = false,
                StartDate = chart.Data.First().Time,
                EndDate = chart.Data.Last().Time,
            };
        }

        private static GraphType ToGraphType(SeriesType seriesType)
        {
            switch (seriesType)
            {
                case SeriesType.Bar:
                    return GraphType.Column;
                case SeriesType.Candle:
                    return GraphType.Candlestick;
                case SeriesType.Line:
                    return GraphType.Line;
                default:
                    return GraphType.Step;
            }
        }

        public static System.Windows.Media.Brush ToMediaBrush(System.Drawing.Color color)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, color.R, color.G, color.B));
        }
    }
}