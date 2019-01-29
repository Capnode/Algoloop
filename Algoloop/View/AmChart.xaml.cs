using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AmCharts.Windows.Stock;
using AmCharts.Windows.Stock.Data;
using QuantConnect;

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
            var charts = e.NewValue as IReadOnlyList<ChartViewModel>;
            if (amChart != null && charts != null)
            {
                amChart.OnItemsSourceChanged(charts);
            }
        }

        private void OnItemsSourceChanged(IReadOnlyList<ChartViewModel> charts)
        {
            // Clear charts
            stockChart.DataSets.Clear();
            Debug.Assert(stockChart.Charts.Count == 1);
            stockChart.Charts[0].Graphs.Clear();

            Visibility visibility = Visibility.Visible;
            foreach (ChartViewModel chart in charts)
            {
                int ix = 0;
                int size = chart.Series.Values.Count;
                KeyValuePair<DateTime, double>[] timeseries = new KeyValuePair<DateTime, double>[size];
                foreach (ChartPoint point in chart.Series.Values)
                {
                    DateTime time = Time.UnixTimeStampToDateTime(point.x);
                    timeseries[ix++] = new KeyValuePair<DateTime, double>(time, (double)point.y);
                }

                var dataset = new DataSet()
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