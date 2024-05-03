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

using Algoloop.Wpf.Model;
using Algoloop.Wpf.Properties;
using Algoloop.Wpf.ViewModels.Internal.Lean;
using Capnode.Wpf.DataGrid;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace Algoloop.Wpf.ViewModels
{
    public class SymbolViewModel : ViewModelBase, ITreeViewModel, IComparable
    {
        public enum ReportPeriod { Year, R12, Quarter };

        private const string Reference = "Reference";
        private const decimal Million = 1e6m;
        private const string Annual = "Annual";
        private const string PeriodEndDate = "End date";
        private const string FileDate = "File date";
        private const string Currency = "Currency";
        private const string TotalRevenue = "Total Revenue (M)";
        private const string OperatingRevenue = "Operating Revenue (M)";
        private const string GrossProfit = "Gross Profit (M)";
        private const string OperatingIncome = "Operating Income (M)";
        private const string PretaxIncome = "Pre Tax Income (M)";
        private const string NetIncome = "Net Income (M)";
        private const string IntangibleAssets = "Intangible Assets (M)";
        private const string TangibleAssets = "Tangible Assets (M)";
        private const string FinancialAssets = "Financial Assets (M)";
        private const string NonCurrentAsset = "Non Current Assets (M)";
        private const string Cash = "Cash (M)";
        private const string CurrentAssets = "Current Assets (M)";
        private const string TotalAssets = "Total Assets (M)";
        private const string TotalEquity = "Total Equity (M)";
        private const string LongTermDebt = "Long Term Debt (M)";
        private const string CurrentLiabilities = "Current Liabilities (M)";
        private const string NetDebt = "Net Debt (M)";
        private const string OperatingCashFlow = "Operating Cash Flow (M)";
        private const string InvestingCashFlow = "Investing Cash Flow (M)";
        private const string FinancingCashFlow = "Financing Cash Flow (M)";
        private const string FreeCashFlow = "Free Cash Flow (M)";
        private const string EarningsPerShare = "Earnings Per Share";
        private const string DividendPerShare = "Dividend Per Share";
        private const string SharesOutstanding = "Shares Outstanding";
        private const string RevenueGrowth = "Revenue Growth %";
        private const string OperatingIncomeGrowth = "Operating Income Growth %";
        private const string NetIncomeGrowth = "Net Income Growth %";
        private const string NetMargin = "Net Margin %";
        private const string PeRatio = "PE Ratio";
        private const string PsRatio = "PS Ratio";

        private readonly IChartViewModel _candleChart;
        private readonly ChartViewModel _referenceChart;
        private readonly ITreeViewModel _parent;
        private SyncObservableCollection<IChartViewModel> _charts = new();
        private ObservableCollection<DataGridColumn> _periodColumns = new();
        private ObservableCollection<ReferenceSymbolViewModel> _referenceSymbols;
        private ReferenceSymbolViewModel _selectedRreferenceSymbol;
        private decimal _ask;
        private decimal _bid;
        private decimal _price;

        public SymbolViewModel(ITreeViewModel parent, SymbolModel model)
        {
            _parent = parent;
            Market = _parent as MarketViewModel;
            Model = model;

            _candleChart = new SymbolChartViewModel(this, true);
            _referenceChart = new ChartViewModel(new Chart(Reference), false);
            _charts.Add(_candleChart);
            _charts.Add(_referenceChart);

            DeleteCommand = new RelayCommand(() => DeleteReferenceSymbol(SelectedReferenceSymbol), () => SelectedReferenceSymbol != null);
            StartCommand = new RelayCommand(() => { }, () => false);
            StopCommand = new RelayCommand(() => { }, () => false);
            UpdateCommand = new RelayCommand(
                () => DoLoadData(),
                () => !IsBusy && Market != null);
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public RelayCommand DeleteCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public SymbolModel Model { get; }
        public MarketViewModel Market { get; }
        public SyncObservableCollection<ExDataGridRow> FundamentalRows { get; }
            = new SyncObservableCollection<ExDataGridRow>();

        public decimal Ask
        {
            get => _ask;
            set => SetProperty(ref _ask, value);
        }

        public decimal Bid
        {
            get => _bid;
            set => SetProperty(ref _bid, value);
        }

        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
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

        public bool Active
        {
            get => Model.Active;
            set
            {
                if (Model.Active != value)
                {
                    Model.Active = value;
                    OnPropertyChanged();
                    (_parent as StrategyViewModel)?.Refresh();
                    (_parent as MarketViewModel)?.Refresh();
                }
            }
        }

        public SyncObservableCollection<IChartViewModel> Charts
        {
            get => _charts;
            set => SetProperty(ref _charts, value);
        }

        public ObservableCollection<DataGridColumn> PeriodColumns
        {
            get => _periodColumns;
            set => SetProperty(ref _periodColumns, value);
        }

        public ObservableCollection<ReferenceSymbolViewModel> ReferenceSymbols
        {
            get => _referenceSymbols;
            set => SetProperty(ref _referenceSymbols, value);
        }

        public ReferenceSymbolViewModel SelectedReferenceSymbol
        {
            get => _selectedRreferenceSymbol;
            set
            {
                SetProperty(ref _selectedRreferenceSymbol, value);
                DeleteCommand.NotifyCanExecuteChanged();
            }
        }

        public void Refresh()
        {
            if (Market != null)
            {
                DoLoadData();
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

        public void Update(SymbolModel symbol)
        {
            Model.Update(symbol);
            OnPropertyChanged(nameof(Model));
        }

        public void DataToModel()
        {
            Model.ReferenceSymbols.Clear();
            if (ReferenceSymbols == null) return;
            foreach (ReferenceSymbolViewModel reference in ReferenceSymbols)
            {
                Model.ReferenceSymbols.Add(reference.Model);
            }
        }

        public IEnumerable<BaseData> History()
        {
            return History(Market.ChartResolution, Market.ChartDate);
        }

        public IEnumerable<BaseData> History(Resolution resolution, DateTime date)
        {
            string filename = PriceFilePath(Market, Model, resolution, date);
            if (!File.Exists(filename)) return null;
            var leanDataReader = new LeanDataReader(filename);
            return leanDataReader.Parse().Where(m => m.Time >= date);
        }

        public void AddReferenceSymbol(SymbolViewModel symbol)
        {
            string message = string.Format(
                CultureInfo.InvariantCulture,
                Resources.ReferenceSymbolAdded,
                symbol.Model.Name);
            Messenger.Send(new NotificationMessage(message), 0);

            var model = new ReferenceSymbolModel(symbol.Model.Market, symbol.Model.Name);
            var reference = new ReferenceSymbolViewModel(model, symbol);
            ReferenceSymbols.Add(reference);
            SelectedReferenceSymbol = reference;
            _referenceChart.IsVisible = true;
            BuildReferenceChart();
            var temp = Charts;
            Charts = null;
            Charts = temp;
        }

        private void DeleteReferenceSymbol(ReferenceSymbolViewModel reference)
        {
            ReferenceSymbols.Remove(reference);
            if (ReferenceSymbols.Any())
            {
                SelectedReferenceSymbol = ReferenceSymbols.First();
            }

            BuildReferenceChart();
            var temp = Charts;
            Charts = null;
            Charts = temp;
        }

        private void DoLoadData()
        {
            try
            {
                IsBusy = true;
                if (ReferenceSymbols == null)
                {
                    ReferenceSymbols = CreateReferenceSymbols();
                    SelectedReferenceSymbol = ReferenceSymbols.FirstOrDefault();
                }

                BuildReferenceChart();
                LoadFundamentals();

                var temp = Charts;
                Charts = null;
                Charts = temp;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private ObservableCollection<ReferenceSymbolViewModel> CreateReferenceSymbols()
        {
            var referenceSymbols = new ObservableCollection<ReferenceSymbolViewModel>();
            foreach (ReferenceSymbolModel model in Model.ReferenceSymbols)
            {
                SymbolViewModel symbol = Market.FindSymbol(model.Market, model.Name);
                if (symbol == null) continue;
                var reference = new ReferenceSymbolViewModel(model, symbol);
                referenceSymbols.Add(reference);
            }

            return referenceSymbols;
        }

        private void BuildReferenceChart()
        {
            _referenceChart.Chart.Series.Clear();
            foreach (ReferenceSymbolViewModel reference in ReferenceSymbols)
            {
                Series series = reference.ChartSeries(this);
                if (series == null) continue;

                // Create unique series name
                int index = 0;
                string name = series.Name;
                while (_referenceChart.Chart.Series.Any(m => m.Key.Equals(name)))
                {
                    index++;
                    name = $"{series.Name} ({index})";
                }

                series.Name = name;
                _referenceChart.Chart.AddSeries(series);
            }

            _referenceChart.IsVisible = ReferenceSymbols.Any();
        }

        private static string PriceFilePath(
            MarketViewModel market,
            SymbolModel symbol,
            Resolution resolution,
            DateTime date)
        {
            var basedir = new DirectoryInfo(market.DataFolder);
            if (resolution.Equals(
                Resolution.Daily) || resolution.Equals(Resolution.Hour))
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
                string date1 = date.ToString(
                    "yyyyMMdd", CultureInfo.InvariantCulture);
                string file = dir
                    .GetFiles(date1 + "*.zip")
                    .Select(x => x.FullName)
                    .FirstOrDefault();
                return file;
            }
        }

        private void LoadFundamentals()
        {
            // Reset Fundamentals
            FundamentalRows.Clear();
            PeriodColumns.Clear();
            ExDataGridColumns.AddTextColumn(
                PeriodColumns, "Item", "Header", false, true);

            // Find FineFundamentals folder
            string folder = Path.Combine(
                Market.DataFolder,
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
                using (StreamReader resultStream = Compression.Unzip(
                    file.FullName, jsonFile, out Ionic.Zip.ZipFile zipFile))
                using (zipFile)
                {
                    if (resultStream == null) continue;
                    using JsonReader reader = new JsonTextReader(resultStream);
                    var serializer = new JsonSerializer();
                    FineFundamental fine =
                        serializer.Deserialize<FineFundamental>(reader);
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
                    if (periodType != null && periodType.Equals(
                        Annual, StringComparison.OrdinalIgnoreCase))
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

            if (value is decimal dec && dec.Equals(decimal.MinValue))
            {
                gridRow.Columns[column] = string.Empty;
            }
            else
            {
                gridRow.Columns[column] = value;
            }

            // Create Grid Column and set format if needed
            ExDataGridColumns.AddPropertyColumns(PeriodColumns, gridRow.Columns, "Columns", true, false);
        }

        private void FundamentalYear(FineFundamental fine, string period)
        {
            DateTime periodEndDate = fine.FinancialStatements.PeriodEndingDate;
            DateTime fileDate = fine.FinancialStatements.FileDate;
            string currency = fine.SecurityReference.CurrencyId;
            decimal totalRevenue = Round(fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths, 1 / Million, 4);
            decimal operatingRevenue = Round(fine.FinancialStatements.IncomeStatement.OperatingRevenue.TwelveMonths, 1 / Million, 4);
            decimal revenueGrowth = Round(fine.OperationRatios.RevenueGrowth.OneYear, 100, 4);
            decimal grossProfit = Round(fine.FinancialStatements.IncomeStatement.GrossProfit.TwelveMonths, 1 / Million, 4);
            decimal operatingIncome = Round(fine.FinancialStatements.IncomeStatement.OperatingIncome.TwelveMonths, 1 / Million, 4);
            decimal operatingIncomeGrowth = Round(fine.OperationRatios.OperationIncomeGrowth.OneYear, 100, 4);
            decimal pretaxIncome = Round(fine.FinancialStatements.IncomeStatement.PretaxIncome.TwelveMonths, 1 / Million, 4);
            decimal netIncome = Round(fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths, 1 / Million, 4);
            decimal netIncomeGrowth = Round(fine.OperationRatios.NetIncomeGrowth.OneYear, 100, 4);
            decimal netMargin = Round(fine.OperationRatios.NetMargin.OneYear, 100, 4);
            decimal otherIntangibleAssets = Round(fine.FinancialStatements.BalanceSheet.OtherIntangibleAssets.TwelveMonths, 1 / Million, 4);
            decimal netTangibleAssets = Round(fine.FinancialStatements.BalanceSheet.NetTangibleAssets.TwelveMonths, 1 / Million, 4);
            decimal financialAssets = Round(fine.FinancialStatements.BalanceSheet.FinancialAssets.TwelveMonths, 1 / Million, 4);
            decimal totalNonCurrentAssets = Round(fine.FinancialStatements.BalanceSheet.TotalNonCurrentAssets.TwelveMonths, 1 / Million, 4);
            decimal cashAndCashEquivalents = Round(fine.FinancialStatements.BalanceSheet.CashAndCashEquivalents.TwelveMonths, 1 / Million, 4);
            decimal currentAssets = Round(fine.FinancialStatements.BalanceSheet.CurrentAssets.TwelveMonths, 1 / Million, 4);
            decimal totalAssets = Round(fine.FinancialStatements.BalanceSheet.TotalAssets.TwelveMonths, 1 / Million, 4);
            decimal totalEquity = Round(fine.FinancialStatements.BalanceSheet.TotalEquity.TwelveMonths, 1 / Million, 4);
            decimal longTermDebt = Round(fine.FinancialStatements.BalanceSheet.LongTermDebt.TwelveMonths, 1 / Million, 4);
            decimal currentLiabilities = Round(fine.FinancialStatements.BalanceSheet.CurrentLiabilities.TwelveMonths, 1 / Million, 4);
            decimal netDebt = Round(fine.FinancialStatements.BalanceSheet.NetDebt.TwelveMonths, 1 / Million, 4);
            decimal operatingCashFlow = Round(fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths, 1 / Million, 4);
            decimal investingCashFlow = Round(fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.TwelveMonths, 1 / Million, 4);
            decimal financingCashFlow = Round(fine.FinancialStatements.CashFlowStatement.FinancingCashFlow.TwelveMonths, 1 / Million, 4);
            decimal freeCashFlow = Round(fine.FinancialStatements.CashFlowStatement.FreeCashFlow.TwelveMonths, 1 / Million, 4);
            decimal eps = Round(fine.EarningReports.BasicEPS.TwelveMonths, 1, 4);
            decimal dividendPerShare = Round(fine.EarningReports.DividendPerShare.TwelveMonths, 1, 4);
            decimal peRatio = Round(fine.ValuationRatios.PERatio, 1, 4);
            decimal psRatio = Round(fine.ValuationRatios.PSRatio, 1, 4);
            decimal sharesOutstanding = fine.CompanyProfile.SharesOutstanding;

            SetFundamentals(PeriodEndDate, period, periodEndDate.ToShortDateString());
            SetFundamentals(FileDate, period, fileDate.ToShortDateString());
            SetFundamentals(Currency, period, currency);
            SetFundamentals(TotalRevenue, period, totalRevenue);
            SetFundamentals(OperatingRevenue, period, operatingRevenue);
            SetFundamentals(RevenueGrowth, period, revenueGrowth);
            SetFundamentals(GrossProfit, period, grossProfit);
            SetFundamentals(OperatingIncome, period, operatingIncome);
            SetFundamentals(OperatingIncomeGrowth, period, operatingIncomeGrowth);
            SetFundamentals(PretaxIncome, period, pretaxIncome);
            SetFundamentals(NetIncome, period, netIncome);
            SetFundamentals(NetIncomeGrowth, period, netIncomeGrowth);
            SetFundamentals(NetMargin, period, netMargin);
            SetFundamentals(IntangibleAssets, period, otherIntangibleAssets);
            SetFundamentals(TangibleAssets, period, netTangibleAssets);
            SetFundamentals(FinancialAssets, period, financialAssets);
            SetFundamentals(NonCurrentAsset, period, totalNonCurrentAssets);
            SetFundamentals(Cash, period, cashAndCashEquivalents);
            SetFundamentals(CurrentAssets, period, currentAssets);
            SetFundamentals(TotalAssets, period, totalAssets);
            SetFundamentals(TotalEquity, period, totalEquity);
            SetFundamentals(LongTermDebt, period, longTermDebt);
            SetFundamentals(CurrentLiabilities, period, currentLiabilities);
            SetFundamentals(NetDebt, period, netDebt);
            SetFundamentals(OperatingCashFlow, period, operatingCashFlow);
            SetFundamentals(InvestingCashFlow, period, investingCashFlow);
            SetFundamentals(FinancingCashFlow, period, financingCashFlow);
            SetFundamentals(FreeCashFlow, period, freeCashFlow);
            SetFundamentals(EarningsPerShare, period, eps);
            SetFundamentals(DividendPerShare, period, dividendPerShare);
            SetFundamentals(PeRatio, period, peRatio);
            SetFundamentals(PsRatio, period, psRatio);
            SetFundamentals(SharesOutstanding, period, sharesOutstanding);
        }

        private void FundamentalQuarter(FineFundamental fine, string period)
        {
            DateTime periodEndDate = fine.FinancialStatements.PeriodEndingDate;
            DateTime fileDate = fine.FinancialStatements.FileDate;
            string currency = fine.SecurityReference.CurrencyId;
            decimal totalRevenue = Round(fine.FinancialStatements.IncomeStatement.TotalRevenue.ThreeMonths, 1 / Million, 4);
            decimal operatingRevenue = Round(fine.FinancialStatements.IncomeStatement.OperatingRevenue.ThreeMonths, 1 / Million, 4);
            decimal revenueGrowth = Round(fine.OperationRatios.RevenueGrowth.ThreeMonths, 100, 4);
            decimal grossProfit = Round(fine.FinancialStatements.IncomeStatement.GrossProfit.ThreeMonths, 1 / Million, 4);
            decimal operatingIncome = Round(fine.FinancialStatements.IncomeStatement.OperatingIncome.ThreeMonths, 1 / Million, 4);
            decimal operatingIncomeGrowth = Round(fine.OperationRatios.OperationIncomeGrowth.ThreeMonths, 100, 4);
            decimal pretaxIncome = Round(fine.FinancialStatements.IncomeStatement.PretaxIncome.ThreeMonths, 1 / Million, 4);
            decimal netIncome = Round(fine.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths, 1 / Million, 4);
            decimal netIncomeGrowth = Round(fine.OperationRatios.NetIncomeGrowth.ThreeMonths, 100, 4);
            decimal netMargin = Round(fine.OperationRatios.NetMargin.ThreeMonths, 100, 4);
            decimal otherIntangibleAssets = Round(fine.FinancialStatements.BalanceSheet.OtherIntangibleAssets.ThreeMonths, 1 / Million, 4);
            decimal netTangibleAssets = Round(fine.FinancialStatements.BalanceSheet.NetTangibleAssets.ThreeMonths, 1 / Million, 4);
            decimal financialAssets = Round(fine.FinancialStatements.BalanceSheet.FinancialAssets.ThreeMonths, 1 / Million, 4);
            decimal totalNonCurrentAssets = Round(fine.FinancialStatements.BalanceSheet.TotalNonCurrentAssets.ThreeMonths, 1 / Million, 4);
            decimal cashAndCashEquivalents = Round(fine.FinancialStatements.BalanceSheet.CashAndCashEquivalents.ThreeMonths, 1 / Million, 4);
            decimal currentAssets = Round(fine.FinancialStatements.BalanceSheet.CurrentAssets.ThreeMonths, 1 / Million, 4);
            decimal totalAssets = Round(fine.FinancialStatements.BalanceSheet.TotalAssets.ThreeMonths, 1 / Million, 4);
            decimal totalEquity = Round(fine.FinancialStatements.BalanceSheet.TotalEquity.ThreeMonths, 1 / Million, 4);
            decimal longTermDebt = Round(fine.FinancialStatements.BalanceSheet.LongTermDebt.ThreeMonths, 1 / Million, 4);
            decimal currentLiabilities = Round(fine.FinancialStatements.BalanceSheet.CurrentLiabilities.ThreeMonths, 1 / Million, 4);
            decimal netDebt = Round(fine.FinancialStatements.BalanceSheet.NetDebt.ThreeMonths, 1 / Million, 4);
            decimal operatingCashFlow = Round(fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.ThreeMonths, 1 / Million, 4);
            decimal investingCashFlow = Round(fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.ThreeMonths, 1 / Million, 4);
            decimal financingCashFlow = Round(fine.FinancialStatements.CashFlowStatement.FinancingCashFlow.ThreeMonths, 1 / Million, 4);
            decimal freeCashFlow = Round(fine.FinancialStatements.CashFlowStatement.FreeCashFlow.ThreeMonths, 1 / Million, 4);
            decimal eps = Round(fine.EarningReports.BasicEPS.ThreeMonths, 1, 4);
            decimal dividendPerShare = Round(fine.EarningReports.DividendPerShare.ThreeMonths, 1, 4);
            decimal peRatio = Round(fine.ValuationRatios.PERatio, 1, 4);
            decimal psRatio = Round(fine.ValuationRatios.PSRatio, 1, 4);
            decimal sharesOutstanding = fine.CompanyProfile.SharesOutstanding;

            if (totalRevenue == 0
                && netIncome == 0
                && revenueGrowth == 0
                && netIncomeGrowth == 0
                && netMargin == 0)
                return;

            SetFundamentals(PeriodEndDate, period, periodEndDate.ToShortDateString());
            SetFundamentals(FileDate, period, fileDate.ToShortDateString());
            SetFundamentals(Currency, period, currency);
            SetFundamentals(TotalRevenue, period, totalRevenue);
            SetFundamentals(OperatingRevenue, period, operatingRevenue);
            SetFundamentals(OperatingIncomeGrowth, period, operatingIncomeGrowth);
            SetFundamentals(RevenueGrowth, period, revenueGrowth);
            SetFundamentals(GrossProfit, period, grossProfit);
            SetFundamentals(OperatingIncome, period, operatingIncome);
            SetFundamentals(PretaxIncome, period, pretaxIncome);
            SetFundamentals(NetIncome, period, netIncome);
            SetFundamentals(NetIncomeGrowth, period, netIncomeGrowth);
            SetFundamentals(NetMargin, period, netMargin);
            SetFundamentals(IntangibleAssets, period, otherIntangibleAssets);
            SetFundamentals(TangibleAssets, period, netTangibleAssets);
            SetFundamentals(FinancialAssets, period, financialAssets);
            SetFundamentals(NonCurrentAsset, period, totalNonCurrentAssets);
            SetFundamentals(Cash, period, cashAndCashEquivalents);
            SetFundamentals(CurrentAssets, period, currentAssets);
            SetFundamentals(TotalAssets, period, totalAssets);
            SetFundamentals(TotalEquity, period, totalEquity);
            SetFundamentals(LongTermDebt, period, longTermDebt);
            SetFundamentals(CurrentLiabilities, period, currentLiabilities);
            SetFundamentals(NetDebt, period, netDebt);
            SetFundamentals(OperatingCashFlow, period, operatingCashFlow);
            SetFundamentals(InvestingCashFlow, period, investingCashFlow);
            SetFundamentals(FinancingCashFlow, period, financingCashFlow);
            SetFundamentals(FreeCashFlow, period, freeCashFlow);
            SetFundamentals(EarningsPerShare, period, eps);
            SetFundamentals(DividendPerShare, period, dividendPerShare);
            SetFundamentals(PeRatio, period, peRatio);
            SetFundamentals(PsRatio, period, psRatio);
            SetFundamentals(SharesOutstanding, period, sharesOutstanding);
        }

        private static decimal Round(decimal value, decimal multiplier, int decimals)
        {
            if (value.Equals(decimal.MinValue)) return decimal.MinValue;
            return decimal.Round(value * multiplier, decimals);
        }
    }
}
