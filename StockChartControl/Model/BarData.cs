using System;

namespace StockChartControl.Model
{
    public class BarData
    {
        public DateTime TradeDateTime;
        public double? OpenPrice;
        public double? HighPrice;
        public double? LowPrice;
        public double? ClosePrice;
        public int? Volume;
    }
}
