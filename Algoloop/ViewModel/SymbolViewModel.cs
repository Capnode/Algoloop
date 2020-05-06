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
using Algoloop.ViewSupport;
using Capnode.Wpf.DataGrid;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Ionic.Zip;
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.ToolBox;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace Algoloop.ViewModel
{
    public class SymbolViewModel : ViewModelBase, ITreeViewModel, IComparable
    {
        public enum ReportPeriod { Year, R12, Quarter};

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
        private Resolution _selectedResolution = Resolution.Daily;
        private SyncObservableCollection<ChartViewModel> _charts = new SyncObservableCollection<ChartViewModel>();
        private ObservableCollection<DataGridColumn> _periodColumns = new ObservableCollection<DataGridColumn>();
        private DateTime _date = DateTime.Today;
        private static ReportPeriod _selectedReportPeriod;

        public SymbolViewModel(ITreeViewModel parent, SymbolModel model)
        {
            _parent = parent;
            Model = model;

            DeleteCommand = new RelayCommand(() => { }, () => false);
            StartCommand = new RelayCommand(() => { }, () => false);
            StopCommand = new RelayCommand(() => { }, () => false);
            UpdateCommand = new RelayCommand(() => DoLoadData(_parent as MarketViewModel), () => !IsBusy && _parent is MarketViewModel);
        }

        public bool IsBusy
        {
            get => _parent.IsBusy;
            set => _parent.IsBusy = value;
        }

        public RelayCommand DeleteCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public SymbolModel Model { get; }

        public IEnumerable<Resolution> ResolutionList { get; } = new[] { Resolution.Daily, Resolution.Hour, Resolution.Minute, Resolution.Second, Resolution.Tick };
        public IEnumerable<ReportPeriod> ReportPeriodList { get; } = new[] { ReportPeriod.Year, ReportPeriod.R12, ReportPeriod.Quarter };
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

        public Resolution SelectedResolution
        {
            get => _selectedResolution;
            set => Set(ref _selectedResolution, value);
        }

        public DateTime Date
        {
            get => _date;
            set => Set(ref _date, value);
        }

        public SyncObservableCollection<ChartViewModel> Charts
        {
            get => _charts;
            set => Set(ref _charts, value);
        }

        public ObservableCollection<DataGridColumn> PeriodColumns
        {
            get => _periodColumns;
            set => Set(ref _periodColumns, value);
        }

        public ReportPeriod SelectedReportPeriod
        {
            get => _selectedReportPeriod;
            set => Set(ref _selectedReportPeriod, value);
        }

        public void Refresh()
        {
            Model.Refresh();

            if (_parent is MarketViewModel market)
            {
                DoLoadData(market);
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
            Debug.Assert(market != null);
            Charts.Clear();
            string filename = PriceFilePath(market, Model.Name, SelectedResolution, Date);
            if (filename != null)
            {
                var leanDataReader = new LeanDataReader(filename);
                IEnumerable<BaseData> data = leanDataReader.Parse();
                if (data.Any())
                {
                    var series = new Series(Model.Name, SeriesType.Candle, "$", Color.Black);
                    var viewModel = new ChartViewModel(series, data);
                    Charts.Add(viewModel);
                }
            }
        }

        private static string PriceFilePath(MarketViewModel market, string symbol, Resolution resolution, DateTime date)
        {
            DirectoryInfo basedir = new DirectoryInfo(market.DataFolder);
            DirectoryInfo[] subdirs = basedir.GetDirectories("*");
            if (resolution.Equals(Resolution.Daily) || resolution.Equals(Resolution.Hour))
            {
                foreach (DirectoryInfo folder in subdirs)
                {
                    var path = Path.Combine(folder.FullName, market.Model.Provider, resolution.ToString());
                    if (!Directory.Exists(path))
                        continue;

                    var dir = new DirectoryInfo(path);
                    var files = dir.GetFiles(symbol + "*.zip").Select(x => x.FullName);
                    if (files.Count() == 1)
                    {
                        return files.Single();
                    }
                }
            }
            else
            {
                foreach (DirectoryInfo folder in subdirs)
                {
                    var path = Path.Combine(folder.FullName, market.Model.Provider, resolution.ToString(), symbol);
                    if (!Directory.Exists(path))
                        continue;

                    var dir = new DirectoryInfo(path);
                    string date1 = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                    var files = dir.GetFiles(date1 + "*.zip").Select(x => x.FullName);
                    if (files.Count() == 1)
                    {
                        return files.Single();
                    }
                }
            }

            return null;
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
                SecurityType.Equity.ToString(),
                market.Model.Provider,
                "fundamental",
                "fine",
                Model.Name.ToLowerInvariant());

            DirectoryInfo d = new DirectoryInfo(folder);
            if (!d.Exists)
            {
                return;
            }

            string jsonFile = $"{Model.Name.ToLowerInvariant()}.json";
            FileInfo[] files = d.GetFiles("*.zip");
            foreach (FileInfo file in files)
            {
                using (StreamReader resultStream = Compression.Unzip(file.FullName, jsonFile, out ZipFile zipFile))
                using (zipFile)
                {
                    if (resultStream == null)
                    {
                        continue;
                    }

                    using JsonReader reader = new JsonTextReader(resultStream);
                    JsonSerializer serializer = new JsonSerializer();
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

            switch (SelectedReportPeriod)
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
            decimal totalRevenue = decimal.Round(fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths / _million, 4); ;
            decimal netIncome = decimal.Round(fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths / _million, 4);
            decimal revenueGrowth = decimal.Round(fine.OperationRatios.RevenueGrowth.OneYear * 100, 4);
            decimal netIncomeGrowth = decimal.Round(fine.OperationRatios.NetIncomeGrowth.OneYear * 100, 4);
            decimal netMargin = decimal.Round(fine.OperationRatios.NetMargin.OneYear * 100, 4);
            decimal peRatio = decimal.Round(fine.ValuationRatios.PERatio, 4);
            decimal operatingCashFlow = decimal.Round(fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths / _million, 4);
            decimal investingCashFlow = decimal.Round(fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.TwelveMonths / _million, 4);
            decimal financingCashFlow = decimal.Round(fine.FinancialStatements.CashFlowStatement.FinancingCashFlow.TwelveMonths / _million, 4);
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
            decimal totalRevenue = decimal.Round(fine.FinancialStatements.IncomeStatement.TotalRevenue.ThreeMonths / _million, 4); ;
            decimal netIncome = decimal.Round(fine.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths / _million, 4);
            decimal revenueGrowth = decimal.Round(fine.OperationRatios.RevenueGrowth.ThreeMonths * 100, 4);
            decimal netIncomeGrowth = decimal.Round(fine.OperationRatios.NetIncomeGrowth.ThreeMonths * 100, 4);
            decimal netMargin = decimal.Round(fine.OperationRatios.NetMargin.ThreeMonths * 100, 4);
            decimal peRatio = decimal.Round(fine.ValuationRatios.PERatio, 4);
            decimal operatingCashFlow = decimal.Round(fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.ThreeMonths / _million, 4);
            decimal investingCashFlow = decimal.Round(fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.ThreeMonths / _million, 4);
            decimal financingCashFlow = decimal.Round(fine.FinancialStatements.CashFlowStatement.FinancingCashFlow.ThreeMonths / _million, 4);
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
    }
}
