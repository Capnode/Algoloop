/*
 * Copyright 2018 Capnode AB
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Algoloop.Model;
using Algoloop.Wpf.Common;
using Algoloop.Wpf.ViewSupport;
using Capnode.Wpf.DataGrid;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Data.Fundamental;
using QuantConnect.ToolBox;
using QuantConnect.Util;
using StockSharp.Algo.Candles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace Algoloop.Wpf.ViewModel
{
    public class SymbolViewModel : ViewModel, ITreeViewModel, IComparable
    {
        public enum ReportPeriod { Year, R12, Quarter };

        private const decimal _million = 1e6m;
        private const string _annual = "Annual";

        private const string _periodEndDate = "End date";
        private const string _fileDate = "File date";
        private const string _totalRevenue = "Total Revenue (M)";
        private const string _netIncome = "Net Income (M)";
        private const string _revenueGrowth = "Revenue Growth %";
        private const string _netIncomeGrowth = "Net Income Growth %";
        private const string _netMargin = "Net Margin %";
        private const string _peRatio = "PE Ratio";
        private const string _operatingCashFlow = "Operating cash flow (M)";
        private const string _investingCashFlow = "Investing cash flow (M)";
        private const string _financingCashFlow = "Financing cash flow (M)";
        private const string _sharesOutstanding = "Shares outstanding";

        private readonly ITreeViewModel _parent;
        private SyncObservableCollection<StockChartViewModel> _charts = new();
        private ObservableCollection<DataGridColumn> _periodColumns = new();
        private bool _showCharts;

        public SymbolViewModel(ITreeViewModel parent, SymbolModel model)
        {
            _parent = parent;
            Market = _parent as MarketViewModel;
            Model = model;

            DeleteCommand = new RelayCommand(() => { }, () => false);
            StartCommand = new RelayCommand(() => { }, () => false);
            StopCommand = new RelayCommand(() => { }, () => false);
            UpdateCommand = new RelayCommand(() => DoLoadData(Market), () => !IsBusy && Market != null);
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public bool IsBusy
        {
            get => _parent.IsBusy;
            set => _parent.IsBusy = value;
        }

        public ITreeViewModel SelectedItem
        {
            get => _parent.SelectedItem;
            set => _parent.SelectedItem = value;
        }

        public RelayCommand DeleteCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public SymbolModel Model { get; }
        public MarketViewModel Market { get; }

        public SyncObservableCollection<ExDataGridRow> FundamentalRows { get; } = new SyncObservableCollection<ExDataGridRow>();

        public bool Active
        {
            get => Model.Active;
            set
            {
                if (Model.Active != value)
                {
                    Model.Active = value;
                    RaisePropertyChanged(() => Active);
//                (_parent as StrategyViewModel)?.Refresh();
//                (_parent as MarketViewModel)?.Refresh();
                }
            }
        }

        public bool ShowCharts
        {
            get => _showCharts;
            set => Set(ref _showCharts, value);
        }

        public SyncObservableCollection<StockChartViewModel> Charts
        {
            get => _charts;
            set => Set(ref _charts, value);
        }

        public ObservableCollection<DataGridColumn> PeriodColumns
        {
            get => _periodColumns;
            set => Set(ref _periodColumns, value);
        }

        public void Refresh()
        {
            Model.Refresh();

            if (Market != null)
            {
                DoLoadData(Market);
            }
        }

        public int CompareTo(object obj)
        {
            if (obj is SymbolViewModel other)
            {
                return string.Compare(Model.Name, other.Model.Name, StringComparison.OrdinalIgnoreCase);
            }

            return 0;
        }

        private void DoLoadData(MarketViewModel market)
        {
            try
            {
                IsBusy = true;
                LoadCharts(market);
                LoadFundamentals(market);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadCharts(MarketViewModel market)
        {
            Charts.Clear();
            string filename = PriceFilePath(market, Model, market.SelectedResolution, market.Date);
            if (File.Exists(filename))
            {
                var leanDataReader = new LeanDataReader(filename);
                IEnumerable<Candle> candles = leanDataReader.Parse().ToCandles();
                if (candles.Any())
                {
                    var viewModel = new StockChartViewModel(Model.Name, Color.Black, candles);
                    Charts.Add(viewModel);
                }
                ShowCharts = true;
            }
            else
            {
                ShowCharts = false;
            }
        }

        private static string PriceFilePath(MarketViewModel market, SymbolModel symbol, Resolution resolution, DateTime date)
        {
            var basedir = new DirectoryInfo(market.DataFolder);
            if (resolution.Equals(Resolution.Daily) || resolution.Equals(Resolution.Hour))
            {
                string path = Path.Combine(
                    market.DataFolder,
                    symbol.Security.SecurityTypeToLower(),
                    symbol.Market,
                    resolution.ToString(),
                    symbol.Id + ".zip");
                return path;
            }
            else
            {
                string path = Path.Combine(
                    market.DataFolder,
                    symbol.Security.SecurityTypeToLower(),
                    symbol.Market, resolution.ToString(),
                    symbol.Id);
                if (!Directory.Exists(path)) return null;
                var dir = new DirectoryInfo(path);
                string date1 = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                string file = dir
                    .GetFiles(date1 + "*.zip")
                    .Select(x => x.FullName)
                    .FirstOrDefault();
                return file;
            }
        }

        private void LoadFundamentals(MarketViewModel market)
        {
            // Reset Fundamentals
            FundamentalRows.Clear();
            PeriodColumns.Clear();
            ExDataGridColumns.AddTextColumn(PeriodColumns, "Item", "Header", false, true);

            // Find FineFundamentals folder
            string folder = Path.Combine(
                market.DataFolder,
                Model.Security.SecurityTypeToLower(),
                Model.Market,
                "fundamental",
                "fine",
                Model.Id.ToLowerInvariant());

            var d = new DirectoryInfo(folder);
            if (!d.Exists)
            {
                return;
            }

            string jsonFile = $"{Model.Id.ToLowerInvariant()}.json";
            FileInfo[] files = d.GetFiles("*.zip");
            foreach (FileInfo file in files)
            {
                using (StreamReader resultStream = Compression.Unzip(file.FullName, jsonFile, out Ionic.Zip.ZipFile zipFile))
                using (zipFile)
                {
                    if (resultStream == null)
                    {
                        continue;
                    }

                    using JsonReader reader = new JsonTextReader(resultStream);
                    var serializer = new JsonSerializer();
                    FineFundamental fine = serializer.Deserialize<FineFundamental>(reader);
                    LoadFundamentals(fine);
                }
            }
        }

        private void LoadFundamentals(FineFundamental fine)
        {
            string periodType = fine.FinancialStatements.PeriodType;
            DateTime date = fine.FinancialStatements.PeriodEndingDate;
            if (date.Equals(DateTime.MinValue))
            {
                return;
            }

            switch (Market.SelectedReportPeriod)
            {
                case ReportPeriod.Year:
                    if (periodType != null && periodType.Equals(_annual, StringComparison.OrdinalIgnoreCase))
                    {
                        FundamentalYear(fine, date.Year.ToStringInvariant());
                    }
                    break;

                case ReportPeriod.R12:
                    FundamentalYear(fine, $"{date.Year}-{date.Month}");
                    break;

                case ReportPeriod.Quarter:
                    FundamentalQuarter(fine, $"{date.Year}-{date.Month}");
                    break;
            }
        }

        private void SetFundamentals(string row, string column, object value)
        {
            if (value is decimal dec && dec.Equals(decimal.MinValue)) return;

            ExDataGridRow gridRow = FundamentalRows.SingleOrDefault(m => m.Header.Equals(row, StringComparison.OrdinalIgnoreCase));
            if (gridRow == default)
            {
                gridRow = new ExDataGridRow(row);
                FundamentalRows.Add(gridRow);
            }

            gridRow.Columns[column] = value;

            // Create Grid Column and set format if needed
            ExDataGridColumns.AddPropertyColumns(PeriodColumns, gridRow.Columns, "Columns", true, false);
        }

        private void FundamentalYear(FineFundamental fine, string period)
        {
            DateTime periodEndDate = fine.FinancialStatements.PeriodEndingDate;
            DateTime fileDate = fine.FinancialStatements.FileDate;
            decimal totalRevenue = Round(fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths, 1 / _million, 4); ;
            decimal netIncome = Round(fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths, 1 / _million, 4);
            decimal revenueGrowth = Round(fine.OperationRatios.RevenueGrowth.OneYear, 100, 4);
            decimal netIncomeGrowth = Round(fine.OperationRatios.NetIncomeGrowth.OneYear, 100, 4);
            decimal netMargin = Round(fine.OperationRatios.NetMargin.OneYear, 100, 4);
            decimal peRatio = Round(fine.ValuationRatios.PERatio, 1, 4);
            decimal operatingCashFlow = Round(fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths, 1 / _million, 4);
            decimal investingCashFlow = Round(fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.TwelveMonths, 1 / _million, 4);
            decimal financingCashFlow = Round(fine.FinancialStatements.CashFlowStatement.FinancingCashFlow.TwelveMonths,1 / _million, 4);
            decimal sharesOutstanding = fine.CompanyProfile.SharesOutstanding;

            SetFundamentals(_periodEndDate, period, periodEndDate.ToShortDateString());
            SetFundamentals(_fileDate, period, fileDate.ToShortDateString());
            SetFundamentals(_totalRevenue, period, totalRevenue);
            SetFundamentals(_netIncome, period, netIncome);
            SetFundamentals(_revenueGrowth, period, revenueGrowth);
            SetFundamentals(_netIncomeGrowth, period, netIncomeGrowth);
            SetFundamentals(_netMargin, period, netMargin);
            SetFundamentals(_peRatio, period, peRatio);
            SetFundamentals(_operatingCashFlow, period, operatingCashFlow);
            SetFundamentals(_investingCashFlow, period, investingCashFlow);
            SetFundamentals(_financingCashFlow, period, financingCashFlow);
            SetFundamentals(_sharesOutstanding, period, sharesOutstanding);
        }

        private void FundamentalQuarter(FineFundamental fine, string period)
        {
            DateTime periodEndDate = fine.FinancialStatements.PeriodEndingDate;
            DateTime fileDate = fine.FinancialStatements.FileDate;
            decimal totalRevenue = Round(fine.FinancialStatements.IncomeStatement.TotalRevenue.ThreeMonths, 1 / _million, 4); ;
            decimal netIncome = Round(fine.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths, 1 / _million, 4);
            decimal revenueGrowth = Round(fine.OperationRatios.RevenueGrowth.ThreeMonths, 100, 4);
            decimal netIncomeGrowth = Round(fine.OperationRatios.NetIncomeGrowth.ThreeMonths, 100, 4);
            decimal netMargin = Round(fine.OperationRatios.NetMargin.ThreeMonths, 100, 4);
            decimal peRatio = Round(fine.ValuationRatios.PERatio, 1, 4);
            decimal operatingCashFlow = Round(fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.ThreeMonths, 1 / _million, 4);
            decimal investingCashFlow = Round(fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.ThreeMonths, 1 / _million, 4);
            decimal financingCashFlow = Round(fine.FinancialStatements.CashFlowStatement.FinancingCashFlow.ThreeMonths, 1 / _million, 4);
            decimal sharesOutstanding = fine.CompanyProfile.SharesOutstanding;

            if (totalRevenue == 0
                && netIncome == 0
                && revenueGrowth == 0
                && netIncomeGrowth == 0
                && netMargin == 0)
                return;

            SetFundamentals(_periodEndDate, period, periodEndDate.ToShortDateString());
            SetFundamentals(_fileDate, period, fileDate.ToShortDateString());
            SetFundamentals(_totalRevenue, period, totalRevenue);
            SetFundamentals(_netIncome, period, netIncome);
            SetFundamentals(_revenueGrowth, period, revenueGrowth);
            SetFundamentals(_netIncomeGrowth, period, netIncomeGrowth);
            SetFundamentals(_netMargin, period, netMargin);
            SetFundamentals(_peRatio, period, peRatio);
            SetFundamentals(_operatingCashFlow, period, operatingCashFlow);
            SetFundamentals(_investingCashFlow, period, investingCashFlow);
            SetFundamentals(_financingCashFlow, period, financingCashFlow);
            SetFundamentals(_sharesOutstanding, period, sharesOutstanding);
        }

        private static decimal Round(decimal value, decimal multiplier, int decimals)
        {
            if (value.Equals(decimal.MinValue)) return decimal.MinValue;
            return decimal.Round(value * multiplier, decimals);
        }
    }
}
