using System.Windows;

namespace StockChartControl.Transforms
{
    /// <summary>
    /// Base class for all data transforms.
    /// Defines methods to transform points from data coordinate system to viewport coordinates and vice versa.
    /// Should be immutable.
    /// </summary>
    /// <remarks>
    /// Based on Microsoft.Research.DynamicDataDisplay code.
    /// </remarks>
    interface IDataTransform
    {
        /// <summary>
        /// Transforms the point in data coordinates to viewport coordinates.
        /// </summary>
        /// <param name="pt">The point in data coordinates.</param>
        /// <returns></returns>
        Point DataToViewport(Point pt);
        /// <summary>
        /// Transforms the point in viewport coordinates to data coordinates.
        /// </summary>
        /// <param name="pt">The point in viewport coordinates.</param>
        /// <returns></returns>
        Point ViewportToData(Point pt);
    }
}
