using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using StockChartControl.Enums;
using StockChartControl.Model;
using StockChartControl.Navigation;

namespace StockCharts.TestApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var rts = LoadStockRates("TestDataRTS.txt");
            stockChart.AddChartPanel(new ChartOptions
            {
                Symbol = "RTSI",
                ChartData = rts,
                SeriesType = SeriesType.LineChart
            });
        }

        private static List<BarData> LoadStockRates(string fileName)
        {
            string[] strings = File.ReadAllLines(fileName);

            var res = new List<BarData>(strings.Length - 1);
            for (int i = 1; i < strings.Length; i++)
            {
                string line = strings[i];
                string[] subLines = line.Split('\t');

                DateTime date = DateTime.Parse(subLines[1]);
                double rate = Double.Parse(subLines[5], CultureInfo.InvariantCulture);

                res.Add(new BarData { TradeDateTime = date, ClosePrice = rate });
            }

            return res;
        }

        private void StockChart_OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ChartCommands.ScrollLeft.Execute("", stockChart);
        }

        private void StockChart_OnMouseEnter(object sender, MouseEventArgs e)
        {
            stockChart.Focus(); // To allow keyboard commands to get to the control
        }
    }
}
