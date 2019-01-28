using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AmCharts.Windows.Stock.Data;
using QuantConnect;

namespace Algoloop
{
    public partial class IrregularChart : UserControl
    {
        public DateTime SelectedStartDate { get { return this.stockChart1.Scroller.SelectedStartDate; } }
        public DateTime SelectedEndDate { get { return this.stockChart1.Scroller.SelectedEndDate; } }

        public IrregularChart()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ChartViewModel chart = e.NewValue as ChartViewModel;
            if (chart == null)
                return;

            stockChart1.DataSets.Clear();

            int size = chart.Series.Values.Count;
            KeyValuePair<DateTime, double>[] timeseries = null;
            if (size > 0)
            {
                int ix = 0;
                timeseries = new KeyValuePair<DateTime, double>[size];
                foreach (ChartPoint point in chart.Series.Values)
                {
                    DateTime time = Time.UnixTimeStampToDateTime(point.x);
                    timeseries[ix++] = new KeyValuePair<DateTime, double>(time, (double)point.y);
                }
            }

            var dataset = new DataSet()
            {
                ID = "id",
                Title = chart.Title,
                ShortTitle = "ShortTitle",
                DateMemberPath = "Key",
                ValueMemberPath = "Value",
                IsSelectedForComparison = false,
                ItemsSource = timeseries,
                IsVisibleInCompareDataSetSelector = false,
                IsVisibleInMainDataSetSelector = false,
                StartDate = timeseries.First().Key,
                EndDate = timeseries.Last().Key
            };

            stockChart1.DataSets.Add(dataset);
            stockChart1.PeriodSelector.ZoomToEnd = true;
            stockChart1.StartDate = timeseries.First().Key;
            stockChart1.EndDate = timeseries.Last().Key;
        }
    }
}