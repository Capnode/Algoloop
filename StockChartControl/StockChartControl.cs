using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StockChartControl.Charts;
using StockChartControl.Model;
using StockChartControl.Navigation;

namespace StockChartControl
{
    public class StockChartControl : ContentControl
    {
        private KeyboardNavigation keyboardNavigation;

        static StockChartControl()
        {
            // Used by visual theme
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StockChartControl), new FrameworkPropertyMetadata(typeof(StockChartControl)));
        }

        public StockChartControl()
        {
            keyboardNavigation = new KeyboardNavigation(this);
        }

        public void AddChartPanel(ChartOptions options)
        {
            this.AddChild(new ChartPanel(options));
        }

        /// <summary>
        /// Saves the full chart to a file, including technical analysis.
        /// </summary>
        /// <param name="filename">The file to write to.</param>
        public void SaveAsImage(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException();

            var extension = Path.GetExtension(filename);
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentNullException();
            
            BitmapEncoder bitmapEncoder;
            extension = extension.ToLower();
            if (extension == ".png")
                bitmapEncoder = new PngBitmapEncoder();
            else if (extension == ".jpg" || extension == ".jpeg")
                bitmapEncoder = new JpegBitmapEncoder();
            else if (extension == ".gif")
                bitmapEncoder = new GifBitmapEncoder();
            else if (extension == ".bmp")
                bitmapEncoder = new BmpBitmapEncoder();
            else throw new ArgumentException("Cannot find a BitmapEncoder for this file type.");
            
            var renderTargetBitmap = new RenderTargetBitmap((int)this.ActualWidth, (int)this.ActualHeight, 96, 96, PixelFormats.Pbgra32);

            foreach (var child in LogicalTreeHelper.GetChildren(this))
            {
                if (child is ChartPanel)
                    renderTargetBitmap.Render((ChartPanel)child);
            }

            bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
            using (Stream stream = File.Create(filename))
            {
                bitmapEncoder.Save(stream);
            }
        }
    }
}
