using System.Windows;

namespace StockChartControl.Transforms
{
    public static class CoordinateUtilities
    {
        public static Rect RectZoom(Rect rect, double ratio)
        {
            return RectZoom(rect, rect.GetCenter(), ratio);
        }

        public static Rect RectZoom(Rect rect, double horizontalRatio, double verticalRatio)
        {
            return RectZoom(rect, rect.GetCenter(), horizontalRatio, verticalRatio);
        }

        public static Rect RectZoom(Rect rect, Point zoomCenter, double ratio)
        {
            return RectZoom(rect, zoomCenter, ratio, ratio);
        }

        public static Rect RectZoom(Rect rect, Point zoomCenter, double horizontalRatio, double verticalRatio)
        {
            Rect res = new Rect();
            res.X = zoomCenter.X - (zoomCenter.X - rect.X) * horizontalRatio;
            res.Y = zoomCenter.Y - (zoomCenter.Y - rect.Y) * verticalRatio;
            res.Width = rect.Width * horizontalRatio;
            res.Height = rect.Height * verticalRatio;
            return res;
        }

        public static Rect RectZoomX(Rect rect, Point zoomCenter, double ratio)
        {
            Rect res = rect;
            res.X = zoomCenter.X - (zoomCenter.X - rect.X) * ratio;
            res.Width = rect.Width * ratio;
            return res;
        }

        public static Rect RectZoomY(Rect rect, Point zoomCenter, double ratio)
        {
            Rect res = rect;
            res.Y = zoomCenter.Y - (zoomCenter.Y - rect.Y) * ratio;
            res.Height = rect.Height * ratio;
            return res;
        }
    }
}
