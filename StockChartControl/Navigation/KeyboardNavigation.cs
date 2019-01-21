using System.Windows;
using System.Windows.Input;
using StockChartControl.Charts;

namespace StockChartControl.Navigation
{
    /// <summary>
    /// Provides keyboard navigation around chart.
    /// </summary>
    public class KeyboardNavigation
    {
        private StockChartControl StockChartControl;

        public KeyboardNavigation(StockChartControl stockChartControl)
        {
            this.StockChartControl = stockChartControl;
            InitCommands();
        }

        private void InitCommands()
        {
            #region Scroll
            var ScrollLeftCommandBinding = new CommandBinding(
                ChartCommands.ScrollLeft,
                ScrollLeftExecute,
                ScrollLeftCanExecute);
            StockChartControl.CommandBindings.Add(ScrollLeftCommandBinding);

            var ScrollRightCommandBinding = new CommandBinding(
                ChartCommands.ScrollRight,
                ScrollRightExecute,
                ScrollRightCanExecute);
            StockChartControl.CommandBindings.Add(ScrollRightCommandBinding);

            var ScrollUpCommandBinding = new CommandBinding(
                ChartCommands.ScrollUp,
                ScrollUpExecute,
                ScrollUpCanExecute);
            StockChartControl.CommandBindings.Add(ScrollUpCommandBinding);

            var ScrollDownCommandBinding = new CommandBinding(
                ChartCommands.ScrollDown,
                ScrollDownExecute,
                ScrollDownCanExecute);
            StockChartControl.CommandBindings.Add(ScrollDownCommandBinding);
            #endregion

            #region Zoom
            var ZoomOutToMouseCommandBinding = new CommandBinding(
                ChartCommands.ZoomOutToMouse,
                ZoomOutToMouseExecute,
                ZoomOutToMouseCanExecute);
            StockChartControl.CommandBindings.Add(ZoomOutToMouseCommandBinding);

            var ZoomInToMouseCommandBinding = new CommandBinding(
                ChartCommands.ZoomInToMouse,
                ZoomInToMouseExecute,
                ZoomInToMouseCanExecute);
            StockChartControl.CommandBindings.Add(ZoomInToMouseCommandBinding);

            var ZoomWithParamCommandBinding = new CommandBinding(
                ChartCommands.ZoomWithParameter,
                ZoomWithParamExecute,
                ZoomWithParamCanExecute);
            StockChartControl.CommandBindings.Add(ZoomWithParamCommandBinding);

            var ZoomInCommandBinding = new CommandBinding(
                ChartCommands.ZoomIn,
                ZoomInExecute,
                ZoomInCanExecute);
            StockChartControl.CommandBindings.Add(ZoomInCommandBinding);

            var ZoomOutCommandBinding = new CommandBinding(
                ChartCommands.ZoomOut,
                ZoomOutExecute,
                ZoomOutCanExecute);
            StockChartControl.CommandBindings.Add(ZoomOutCommandBinding);

            var FitToViewCommandBinding = new CommandBinding(
                ChartCommands.FitToView,
                FitToViewExecute,
                FitToViewCanExecute);
            StockChartControl.CommandBindings.Add(FitToViewCommandBinding);
            #endregion
        }

        #region Scroll

        private double scrollCoeff = 0.05;
        private void ScrollVisibleProportionally(double xShiftCoeff, double yShiftCoeff)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(StockChartControl))
            {
                if (child is ChartPanel)
                    ((ChartPanel)child).ScrollVisible(xShiftCoeff, yShiftCoeff);
            }
        }

        #region ScrollLeft

        private void ScrollLeftExecute(object target, ExecutedRoutedEventArgs e)
        {
            ScrollVisibleProportionally(scrollCoeff, 0);
            e.Handled = true;
        }

        private void ScrollLeftCanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion

        #region ScrollRight

        private void ScrollRightExecute(object target, ExecutedRoutedEventArgs e)
        {
            ScrollVisibleProportionally(-scrollCoeff, 0);
            e.Handled = true;
        }

        private void ScrollRightCanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion

        #region ScrollUp

        private void ScrollUpExecute(object target, ExecutedRoutedEventArgs e)
        {
            ScrollVisibleProportionally(0, -scrollCoeff);
            e.Handled = true;
        }

        private void ScrollUpCanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion

        #region ScrollDown

        private void ScrollDownExecute(object target, ExecutedRoutedEventArgs e)
        {
            ScrollVisibleProportionally(0, scrollCoeff);
            e.Handled = true;
        }

        private void ScrollDownCanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion

        #endregion

        #region Zoom

        private void ZoomToPoint(double coeff)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(StockChartControl))
            {
                if (child is ChartPanel)
                    ((ChartPanel)child).ZoomToPoint(coeff);
            }
        }

        #region Zoom In To Mouse

        private void ZoomInToMouseExecute(object target, ExecutedRoutedEventArgs e)
        {
            ZoomToPoint(zoomInCoeff);
            e.Handled = true;
        }

        private void ZoomInToMouseCanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion

        #region Zoom Out To Mouse

        private void ZoomOutToMouseExecute(object target, ExecutedRoutedEventArgs e)
        {
            ZoomToPoint(zoomOutCoeff);
            e.Handled = true;
        }

        private void ZoomOutToMouseCanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion

        #region Zoom With param

        private void ZoomWithParamExecute(object target, ExecutedRoutedEventArgs e)
        {
            double zoomParam = (double)e.Parameter;

            foreach (var child in LogicalTreeHelper.GetChildren(StockChartControl))
            {
                if (child is ChartPanel)
                    ((ChartPanel)child).ZoomWithParamExecute(zoomParam);
            }

            e.Handled = true;
        }

        private void ZoomWithParamCanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion

        #region Zoom in

        private double zoomInCoeff = 0.9;
        private void ZoomInExecute(object target, ExecutedRoutedEventArgs e)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(StockChartControl))
            {
                if (child is ChartPanel)
                    ((ChartPanel)child).Zoom(zoomInCoeff);
            }

            e.Handled = true;
        }

        private void ZoomInCanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion

        #region Zoom out

        private double zoomOutCoeff = 1.1;
        private void ZoomOutExecute(object target, ExecutedRoutedEventArgs e)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(StockChartControl))
            {
                if (child is ChartPanel)
                    ((ChartPanel)child).Zoom(zoomOutCoeff);
            }

            e.Handled = true;
        }

        private void ZoomOutCanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion

        #region Fit to view

        private void FitToViewExecute(object target, ExecutedRoutedEventArgs e)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(StockChartControl))
            {
                if (child is ChartPanel)
                    ((ChartPanel)child).FitToView();
            }

            e.Handled = true;
        }

        private void FitToViewCanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion

        #endregion
    }
}
