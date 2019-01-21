using System.Windows;

namespace StockChartControl.Transforms
{
    /// <summary>
    /// Represents identity data transform, which applies no transformation.
    /// </summary>
    class IdentityTransform : IDataTransform
    {
        public Point DataToViewport(Point pt)
        {
            return pt;
        }

        public Point ViewportToData(Point pt)
        {
            return pt;
        }
    }
}
