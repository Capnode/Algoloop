using System;
using System.Collections.Generic;
using StockChartControl.Enums;

namespace StockChartControl.Model
{
    /// <summary>
    /// Represents a collection of chart options destined to initialize a ChartPanel.
    /// </summary>
    public class ChartOptions
    {
        public String Symbol { get; set; }
        public List<BarData> ChartData { get; set; }
        public SeriesType SeriesType { get; set; }
        public IndicatorType? IndicatorType { get; set; }
        public ChartStyle ChartStyle { get; set; }
    }
}
