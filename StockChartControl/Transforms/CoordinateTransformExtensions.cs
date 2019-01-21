using System.Collections.Generic;
using System.Windows;

namespace StockChartControl.Transforms
{

    public static class CoordinateTransformExtensions
    {
        /// <summary>
        /// Transforms list of points from data coordinates to screen coordinates.
        /// </summary>
        /// <param name="transform">Coordinate transform used to perform transformation</param>
        /// <param name="dataPoints">Points in data coordinates</param>
        /// <returns>List of points in screen coordinates</returns>
        public static List<Point> DataToScreen(this CoordinateTransform transform, IEnumerable<Point> dataPoints)
        {
            return dataPoints.DataToScreen(transform);
        }

        /// <summary>
        /// Transforms list of points from data coordinates to screen coordinates.
        /// </summary>
        /// <param name="dataPoints">Points in data coordinates</param>
        /// <param name="transform">CoordinateTransform used to perform transformation</param>
        /// <returns>Points in screen coordinates</returns>
        public static List<Point> DataToScreen(this IEnumerable<Point> dataPoints, CoordinateTransform transform)
        {
            var collection = dataPoints as ICollection<Point>;
            List<Point> res;

            if (collection != null)
            {
                res = new List<Point>(collection.Count);
            }
            else
            {
                res = new List<Point>();
            }

            foreach (var point in dataPoints)
            {
                res.Add(transform.DataToScreen(point));
            }

            return res;
        }
    }
}
