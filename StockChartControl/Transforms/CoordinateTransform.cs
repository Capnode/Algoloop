using System.Windows;

namespace StockChartControl.Transforms
{
    /// <summary>
    /// Provides methods to transform points from one coordinate system to another.
    /// </summary>
    /// <remarks>
    /// Based on Microsoft.Research.DynamicDataDisplay code.
    /// </remarks>
    public class CoordinateTransform
    {
        private double rxToScreen;
        private double ryToScreen;
        private double cxToScreen;
        private double cyToScreen;
        
        private double rxToData;
        private double ryToData;
        private double cxToData;
        private double cyToData;

        private IDataTransform dataTransform = new IdentityTransform();

        public CoordinateTransform(Rect visibleRect, Rect screenRect)
        {
            rxToScreen = screenRect.Width / visibleRect.Width;
            ryToScreen = screenRect.Height / visibleRect.Height;
            cxToScreen = visibleRect.Left * rxToScreen - screenRect.Left;
            cyToScreen = screenRect.Height + screenRect.Top + visibleRect.Top * ryToScreen;

            rxToData = visibleRect.Width / screenRect.Width;
            ryToData = visibleRect.Height / screenRect.Height;
            cxToData = screenRect.Left * rxToData - visibleRect.Left;
            cyToData = visibleRect.Height + visibleRect.Top + screenRect.Top * ryToData;
        }

        internal CoordinateTransform WithRects(Rect visibleRect, Rect screenRect)
        {
            var copy = new CoordinateTransform(visibleRect, screenRect);
            copy.dataTransform = dataTransform;
            return copy;
        }

        /// <summary>
        /// Transforms point from data coordinates to screen.
        /// </summary>
        /// <param name="dataPoint">The point in data coordinates.</param>
        /// <returns></returns>
        public Point DataToScreen(Point dataPoint)
        {
            Point viewportPoint = dataTransform.DataToViewport(dataPoint);

            Point screenPoint = new Point(viewportPoint.X * rxToScreen - cxToScreen, cyToScreen - viewportPoint.Y * ryToScreen);

            return screenPoint;
        }

        /// <summary>
        /// Transforms point from screen coordinates to data coordinates.
        /// </summary>
        /// <param name="screenPoint">The point in screen coordinates.</param>
        /// <returns></returns>
        public Point ScreenToData(Point screenPoint)
        {
            Point viewportPoint = new Point(screenPoint.X * rxToData - cxToData, cyToData - screenPoint.Y * ryToData);

            Point dataPoint = dataTransform.ViewportToData(viewportPoint);

            return dataPoint;
        }

        /// <summary>
        /// Transforms point from viewport coordinates to screen coordinates.
        /// </summary>
        /// <param name="viewportPoint">The point in viewport coordinates.</param>
        /// <returns></returns>
        public Point ViewportToScreen(Point viewportPoint)
        {
            Point screenPoint = new Point(viewportPoint.X * rxToScreen - cxToScreen, cyToScreen - viewportPoint.Y * ryToScreen);

            return screenPoint;
        }

        /// <summary>
        /// Transforms point from screen coordinates to viewport coordinates.
        /// </summary>
        /// <param name="screenPoint">The point in screen coordinates.</param>
        /// <returns></returns>
        public Point ScreenToViewport(Point screenPoint)
        {
            Point viewportPoint = new Point(screenPoint.X * rxToData - cxToData, cyToData - screenPoint.Y * ryToData);

            return viewportPoint;
        }

        internal static CoordinateTransform CreateDefault()
        {
            return new CoordinateTransform(new Rect(0, 0, 1, 1), new Rect(0, 0, 1, 1));
        }
    }
}
