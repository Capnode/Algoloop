using System.Windows.Media;

namespace StockChartControl.Model
{
    /// <summary>
    /// Chart style options.
    /// </summary>
    public class ChartStyle
    {
        /// <summary>
        /// Chart background color.
        /// </summary>
        public Brush BackgroundColor { get; set; }

        /// <summary>
        /// Line chart line color.
        /// </summary>
        public Brush LineColor { get; set; }
    }
}
