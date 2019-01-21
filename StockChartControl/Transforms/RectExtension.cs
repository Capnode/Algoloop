using System.Windows;

namespace StockChartControl.Transforms
{
    public static class RectExtension
    {
        public static Point GetCenter(this Rect rect)
        {
            return new Point(rect.Left + rect.Width * 0.5, rect.Top + rect.Height * 0.5);
        }

        public static Rect FromCenterSize(Point center, Size size)
        {
            return FromCenterSize(center, size.Width, size.Height);
        }

        public static Rect FromCenterSize(Point center, double width, double height)
        {
            var res = new Rect(center.X - width / 2, center.Y - height / 2, width, height);
            return res;
        }

        public static Rect Zoom(this Rect rect, Point to, double ratio)
        {
            return CoordinateUtilities.RectZoom(rect, to, ratio);
        }

        public static Rect ZoomX(this Rect rect, Point to, double ratio)
        {
            return CoordinateUtilities.RectZoomX(rect, to, ratio);
        }

        public static Rect ZoomY(this Rect rect, Point to, double ratio)
        {
            return CoordinateUtilities.RectZoomY(rect, to, ratio);
        }
    }
}
