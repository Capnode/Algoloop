using System.Windows.Input;

namespace StockChartControl.Navigation
{
    public static class ChartCommands
    {
        private static RoutedUICommand CreateCommand(string name, params Key[] keys)
        {
            var gestures = new InputGestureCollection();
            foreach (var key in keys)
                gestures.Add(new KeyGesture(key));

            return new RoutedUICommand(name, name, typeof(ChartCommands), gestures);
        }

        private static RoutedUICommand CreateCommand(string name, MouseAction mouseAction)
        {
            return new RoutedUICommand(name, name, typeof(ChartCommands), new InputGestureCollection { new MouseGesture(mouseAction) });
        }

        #region Commands

        #region Scroll
        private static readonly RoutedUICommand scrollLeft = CreateCommand("ScrollLeft", Key.Right);
        public static RoutedUICommand ScrollLeft
        {
            get { return ChartCommands.scrollLeft; }
        }

        private static readonly RoutedUICommand scrollRight = CreateCommand("ScrollRight", Key.Left);
        public static RoutedUICommand ScrollRight
        {
            get { return ChartCommands.scrollRight; }
        }

        private static readonly RoutedUICommand scrollUp = CreateCommand("ScrollUp", Key.Down);
        public static RoutedUICommand ScrollUp
        {
            get { return ChartCommands.scrollUp; }
        }

        private static readonly RoutedUICommand scrollDown = CreateCommand("ScrollDown", Key.Up);
        public static RoutedUICommand ScrollDown
        {
            get { return ChartCommands.scrollDown; }
        }
        #endregion

        #region Zoom
        private static readonly RoutedUICommand zoomOutToMouse = CreateCommand("ZoomOutToMouse", MouseAction.RightDoubleClick);
        public static RoutedUICommand ZoomOutToMouse
        {
            get { return ChartCommands.zoomOutToMouse; }
        }

        private static readonly RoutedUICommand zoomInToMouse = CreateCommand("ZoomInToMouse", MouseAction.LeftDoubleClick);
        public static RoutedUICommand ZoomInToMouse
        {
            get { return ChartCommands.zoomInToMouse; }
        }

        private static readonly RoutedUICommand zoomWithParam = CreateCommand("ZoomWithParam");
        public static RoutedUICommand ZoomWithParameter
        {
            get { return zoomWithParam; }
        }

        private static readonly RoutedUICommand zoomIn = CreateCommand("ZoomIn", Key.Add);
        public static RoutedUICommand ZoomIn
        {
            get { return zoomIn; }
        }

        private static readonly RoutedUICommand zoomOut = CreateCommand("ZoomOut", Key.Subtract);
        public static RoutedUICommand ZoomOut
        {
            get { return zoomOut; }
        }

        private static readonly RoutedUICommand fitToView = CreateCommand("FitToView", Key.Home);
        public static RoutedUICommand FitToView
        {
            get { return ChartCommands.fitToView; }
        }
        #endregion

        #endregion
    }
}
