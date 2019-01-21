using System.Windows.Media;
using StockChartControl.Model;

namespace StockChartControl.Themes.ChartStyles
{
    public class DefaultChartStyle : ChartStyle
    {
        public DefaultChartStyle()
        {
            BackgroundColor     = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            LineColor           = Brushes.BlueViolet;
        }
    }
}
