/*
 * Copyright 2020 Capnode AB
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
using Borsdata.Api.Dal;
using Borsdata.Api.Dal.Infrastructure;
using Borsdata.Api.Dal.Model;
using Ionic.Zip;
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Algoloop.ToolBox.BorsdataDownloader
{
    public class BorsdataDataDownloader : IDataDownloader, IDisposable
    {
        private const string Annual = "Annual";
        private const string Quarter = "Quarter";
        private const double Million = 1e6;
        private const int KpiRevenueGrowth = 94;
        private const int KpiProfitGrowth = 97;
        private const int KpiReportDateLatest = 202;
        private static readonly DateTime FirstDate = new(1997, 01, 01);

        private KpisAllCompRespV1 _lastReports;
        private bool _isDisposed;
        private readonly ApiClient _api;
        private InstrumentRespV1 _allInstruments;
        private StockSplitRespV1 _splits;

        public BorsdataDataDownloader(string apiKey)
        {
            _api = new ApiClient(apiKey);
        }

        public IEnumerable<BaseData> Get(DataDownloaderGetParameters parameters)
        {
            //            Log.Trace($"{GetType().Name}: Get {symbol.Value} {resolution} {startUtc.ToShortDateString()} {startUtc.ToShortTimeString()}");
            Initialize();
            InstrumentV1 inst = _allInstruments.Instruments.Find(m => m.Yahoo.Equals(parameters.Symbol.Value));
            if (inst == default) return null;

            // Reload preliminary data
            TimeZoneInfo cetZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            DateTime afterCet = TimeZoneInfo.ConvertTimeFromUtc(parameters.StartUtc, cetZone);
            if (afterCet < FirstDate)
            {
                afterCet = FirstDate;
            }

            // Data is fullly available after 21 CET
            DateTime after = afterCet.Hour < 21 ? afterCet.AddDays(-1).Date : afterCet.Date;

            // Download reports
            DownloadReports(parameters.Symbol, inst.InsId.Value, after);

            // Download price data
            return DownloadSymbol(parameters.Symbol, inst.InsId.Value, parameters.Resolution, after, parameters.EndUtc);
        }

        public IEnumerable<SymbolModel> GetInstruments()
        {
            if (_allInstruments == default)
            {
                _allInstruments = _api.GetInstruments();
            }

            CountriesRespV1 cr = _api.GetCountries();
            BranchesRespV1 br = _api.GetBranches();
            SectorsRespV1 sr = _api.GetSectors();
            MarketsRespV1 mr = _api.GetMarkets();

            var symbols = new List<SymbolModel>();
            foreach (InstrumentV1 inst in _allInstruments.Instruments)
            {
                inst.CountryModel = cr.Countries.FirstOrDefault(o => o.Id == inst.CountryId);
                inst.MarketModel = mr.Markets.FirstOrDefault(o => o.Id == inst.MarketId);
                inst.BranchModel = br.Branches.FirstOrDefault(o => o.Id == inst.BranchId);
                inst.SectorModel = sr.Sectors.FirstOrDefault(o => o.Id == inst.SectorId);
                SymbolModel symbol = CreateSymbolModel(inst);
                if (symbol == null) continue;
                symbols.Add(symbol);
            }

            return symbols;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _api?.Dispose();
            }

            _isDisposed = true;
        }

        private void Initialize()
        {
            // Load common data
            if (_allInstruments == default)
            {
                _allInstruments = _api.GetInstruments();
            }

            if (_splits == default)
            {
                _splits = _api.GetStockSplits();
            }

            if (_lastReports == default)
            {
                _lastReports = _api.GetKpiScreener(KpiReportDateLatest, TimeType.last, CalcType.latest);
            }
        }

        private static SymbolModel CreateSymbolModel(InstrumentV1 inst)
        {
            if (string.IsNullOrWhiteSpace(inst.Yahoo) || inst.Yahoo.Contains(' ')) return null;

            return new SymbolModel(inst.Yahoo, Market.Borsdata, SecurityType.Equity)
            {
                Name = inst.Name,
                Properties = new Dictionary<string, object>
                    {
                        { "Ticker", inst.Ticker },
                        { "Country", inst.CountryModel?.Name ?? string.Empty },
                        { "Marketplace", inst.MarketModel?.Name ?? string.Empty },
                        { "Sector", inst.SectorModel?.Name ?? string.Empty },
                        { "Branch", inst.BranchModel?.Name ?? string.Empty }
                    }
            };
        }

        private void DownloadReports(
            Symbol symbol,
            long instId,
            DateTime afterUtc)
        {
            // Get latest report date
            IEnumerable<KpiV1> reports = _lastReports.Values.Where(m => m.I.Equals(instId) && !string.IsNullOrEmpty(m.S));
            KpiV1 report = reports.SingleOrDefault();
            DateTime date = report != default ? DateTime.Parse(report.S, CultureInfo.InvariantCulture) : default;

            // Create FineFundamentals folder
            string folder = Path.Combine(
                Globals.DataFolder,
                symbol.ID.SecurityType.SecurityTypeToLower(),
                symbol.ID.Market,
                "fundamental",
                "fine",
                symbol.Value.ToLowerInvariant());

            List<FineFundamental> fundamentals = new();
            if (Directory.Exists(folder))
            {
                // Check if any report after afterUtc
                if (date <= afterUtc) return;

                // Read all reports from file
                DirectoryInfo d = new(folder);
                foreach (FileInfo zipFile in d.GetFiles("*.zip"))
                {
                    FineFundamental fine = ReadFineFundamentalFile(zipFile.FullName, symbol);
                    DateTime endDate = fine.FinancialStatements.PeriodEndingDate;
                    if (fundamentals.Any(m => m.FinancialStatements.PeriodEndingDate.Equals(endDate))) continue;
                    fundamentals.Add(fine);
                }
            }

            Log.Trace($"{GetType().Name}: {date:d} Update report {symbol.Value}");
            //ReportsRespV1 reportsAll = _api.GetReports(instId);

            // Update reports
            ReportsYear(fundamentals, symbol, instId);
            ReportsR12(fundamentals, symbol, instId);
            ReportsQuarter(fundamentals, symbol, instId);

            // Rewrite fundamental files
            WriteFineFundamentalFiles(fundamentals, symbol, folder);
        }

        private void ReportsYear(IList<FineFundamental> fundamentals, Symbol symbol, long instId)
        {
            ReportsYearRespV1 reportsYear = _api.GetReportsYear(instId);
            IOrderedEnumerable<ReportYearV1> orderedReports = reportsYear.Reports.OrderBy(m => m.ReportEndDate);
            foreach (ReportYearV1 report in orderedReports)
            {
                var fine = new FineFundamental { Symbol = symbol };
                fine.SecurityReference.CurrencyId = report.Currency;
                fine.FinancialStatements.PeriodType = Annual;
                fine.FinancialStatements.PeriodEndingDate = report.ReportEndDate ?? default;
                fine.FinancialStatements.FileDate = FileDate(report.Report_Date, report.ReportEndDate);
                fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths = Multiply(report.Net_Sales, Million);
                fine.FinancialStatements.IncomeStatement.OperatingRevenue.TwelveMonths = Multiply(report.Net_Sales, Million);
                fine.FinancialStatements.IncomeStatement.GrossProfit.TwelveMonths = Multiply(report.GrossIncome, Million);
                fine.FinancialStatements.IncomeStatement.OperatingIncome.TwelveMonths = Multiply(report.OperatingIncome, Million);
                fine.FinancialStatements.IncomeStatement.PretaxIncome.TwelveMonths = Multiply(report.ProfitBeforeTax, Million);
                fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths = Multiply(report.ProfitToEquityHolders, Million);
                fine.FinancialStatements.BalanceSheet.OtherIntangibleAssets.TwelveMonths = Multiply(report.IntangibleAssets, Million);
                fine.FinancialStatements.BalanceSheet.NetTangibleAssets.TwelveMonths = Multiply(report.TangibleAssets, Million);
                fine.FinancialStatements.BalanceSheet.FinancialAssets.TwelveMonths = Multiply(report.FinancialAssets, Million);
                fine.FinancialStatements.BalanceSheet.TotalNonCurrentAssets.TwelveMonths = Multiply(report.NonCurrentAssets, Million);
                fine.FinancialStatements.BalanceSheet.CashAndCashEquivalents.TwelveMonths = Multiply(report.CashAndEquivalents, Million);
                fine.FinancialStatements.BalanceSheet.CurrentAssets.TwelveMonths = Multiply(report.CurrentAssets, Million);
                fine.FinancialStatements.BalanceSheet.TotalAssets.TwelveMonths = Multiply(report.TotalAssets, Million);
                fine.FinancialStatements.BalanceSheet.TotalEquity.TwelveMonths = Multiply(report.TotalEquity, Million);
                fine.FinancialStatements.BalanceSheet.LongTermDebt.TwelveMonths = Multiply(report.NonCurrentLiabilities, Million);
                fine.FinancialStatements.BalanceSheet.CurrentLiabilities.TwelveMonths = Multiply(report.CurrentLiabilities, Million);
                fine.FinancialStatements.BalanceSheet.NetDebt.TwelveMonths = Multiply(report.NetDebt, Million);
                fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths = Multiply(report.CashFlowFromOperatingActivities, Million);
                fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.TwelveMonths = Multiply(report.CashFlowFromInvestingActivities, Million);
                fine.FinancialStatements.CashFlowStatement.FinancingCashFlow.TwelveMonths = Multiply(report.CashFlowFromFinancingActivities, Million);
                fine.FinancialStatements.CashFlowStatement.FreeCashFlow.TwelveMonths = Multiply(report.FreeCashFlow, Million);
                fine.EarningReports.BasicEPS.TwelveMonths = Multiply(report.EarningsPerShare, 1);
                fine.EarningReports.DividendPerShare.TwelveMonths = Multiply(report.Dividend, 1);
                fine.OperationRatios.NetMargin.OneYear = Divide(
                    fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths,
                    fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths);

                long shares = (long)((report.NumberOfShares ?? 0) * Million);
                fine.CompanyProfile.SharesOutstanding = shares;
                double earningsPerShare = shares > 0 ? (double)fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths / shares : 0;
                double salsePerShare = shares > 0 ? (double)fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths / shares : 0;
                fine.ValuationRatios.PERatio = Divide(report.StockPriceAverage, earningsPerShare);
                fine.ValuationRatios.PSRatio = Divide(report.StockPriceAverage, salsePerShare);

                DateTime lastYear = fine.FinancialStatements.PeriodEndingDate.AddDays(1).AddYears(-1).AddDays(-1);
                FineFundamental last = fundamentals.LastOrDefault(m => m.FinancialStatements.PeriodEndingDate == lastYear);
                fine.OperationRatios.RevenueGrowth.OneYear = Growth(
                    fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths,
                    last?.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths);
                fine.OperationRatios.OperationIncomeGrowth.OneYear = Growth(
                    fine.FinancialStatements.IncomeStatement.OperatingIncome.TwelveMonths,
                    last?.FinancialStatements.IncomeStatement.OperatingIncome.TwelveMonths);
                fine.OperationRatios.NetIncomeGrowth.OneYear = Growth(
                    fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths,
                    last?.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths);

                UpdateFineFundamental(fundamentals, fine);
            }
        }

        private void ReportsR12(IList<FineFundamental> fundamentals, Symbol symbol, long instId)
        {
            ReportsR12RespV1 reportsR12 = _api.GetReportsR12(instId);
            ReportR12V1[] orderedReports = reportsR12.Reports.OrderBy(m => m.ReportEndDate).ToArray();
            foreach (ReportR12V1 report in orderedReports)
            {
                var fine = new FineFundamental { Symbol = symbol };
                fine.SecurityReference.CurrencyId = report.Currency;
                fine.FinancialStatements.PeriodType = Quarter;
                fine.FinancialStatements.PeriodEndingDate = report.ReportEndDate ?? default;
                fine.FinancialStatements.FileDate = FileDate(report.Report_Date, report.ReportEndDate);
                fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths = Multiply(report.Net_Sales, Million);
                fine.FinancialStatements.IncomeStatement.OperatingRevenue.TwelveMonths = Multiply(report.Net_Sales, Million);
                fine.FinancialStatements.IncomeStatement.GrossProfit.TwelveMonths = Multiply(report.GrossIncome, Million);
                fine.FinancialStatements.IncomeStatement.OperatingIncome.TwelveMonths = Multiply(report.OperatingIncome, Million);
                fine.FinancialStatements.IncomeStatement.PretaxIncome.TwelveMonths = Multiply(report.ProfitBeforeTax, Million);
                fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths = Multiply(report.ProfitToEquityHolders, Million);
                fine.FinancialStatements.BalanceSheet.OtherIntangibleAssets.TwelveMonths = Multiply(report.IntangibleAssets, Million);
                fine.FinancialStatements.BalanceSheet.NetTangibleAssets.TwelveMonths = Multiply(report.TangibleAssets, Million);
                fine.FinancialStatements.BalanceSheet.FinancialAssets.TwelveMonths = Multiply(report.FinancialAssets, Million);
                fine.FinancialStatements.BalanceSheet.TotalNonCurrentAssets.TwelveMonths = Multiply(report.NonCurrentAssets, Million);
                fine.FinancialStatements.BalanceSheet.CashAndCashEquivalents.TwelveMonths = Multiply(report.CashAndEquivalents, Million);
                fine.FinancialStatements.BalanceSheet.CurrentAssets.TwelveMonths = Multiply(report.CurrentAssets, Million);
                fine.FinancialStatements.BalanceSheet.TotalAssets.TwelveMonths = Multiply(report.TotalAssets, Million);
                fine.FinancialStatements.BalanceSheet.TotalEquity.TwelveMonths = Multiply(report.TotalEquity, Million);
                fine.FinancialStatements.BalanceSheet.LongTermDebt.TwelveMonths = Multiply(report.NonCurrentLiabilities, Million);
                fine.FinancialStatements.BalanceSheet.CurrentLiabilities.TwelveMonths = Multiply(report.CurrentLiabilities, Million);
                fine.FinancialStatements.BalanceSheet.NetDebt.TwelveMonths = Multiply(report.NetDebt, Million);
                fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths = Multiply(report.CashFlowFromOperatingActivities, Million);
                fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.TwelveMonths = Multiply(report.CashFlowFromInvestingActivities, Million);
                fine.FinancialStatements.CashFlowStatement.FinancingCashFlow.TwelveMonths = Multiply(report.CashFlowFromFinancingActivities, Million);
                fine.FinancialStatements.CashFlowStatement.FreeCashFlow.TwelveMonths = Multiply(report.FreeCashFlow, Million);
                fine.EarningReports.BasicEPS.TwelveMonths = Multiply(report.EarningsPerShare, 1);
                fine.EarningReports.DividendPerShare.TwelveMonths = Multiply(report.Dividend, 1);
                fine.OperationRatios.NetMargin.OneYear = Divide(
                    fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths,
                    fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths);

                long shares = (long)((report.NumberOfShares ?? 0) * Million);
                fine.CompanyProfile.SharesOutstanding = shares;
                double earningsPerShare = shares > 0 ? (double)fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths / shares : 0;
                double salsePerShare = shares > 0 ? (double)fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths / shares : 0;
                fine.ValuationRatios.PERatio = Divide(report.StockPriceAverage, earningsPerShare);
                fine.ValuationRatios.PSRatio = Divide(report.StockPriceAverage, salsePerShare);

                DateTime lastYear = fine.FinancialStatements.PeriodEndingDate.AddDays(1).AddYears(-1).AddDays(-1);
                FineFundamental last = fundamentals.LastOrDefault(m => m.FinancialStatements.PeriodEndingDate == lastYear);
                fine.OperationRatios.RevenueGrowth.OneYear = Growth(
                    fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths,
                    last?.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths);
                fine.OperationRatios.OperationIncomeGrowth.OneYear = Growth(
                    fine.FinancialStatements.IncomeStatement.OperatingIncome.TwelveMonths,
                    last?.FinancialStatements.IncomeStatement.OperatingIncome.TwelveMonths);
                fine.OperationRatios.NetIncomeGrowth.OneYear = Growth(
                    fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths,
                    last?.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths);

                UpdateFineFundamental(fundamentals, fine);
            }
        }

        private void ReportsQuarter(IList<FineFundamental> fundamentals, Symbol symbol, long instId)
        {
            ReportsQuarterRespV1 reportsQuarter = _api.GetReportsQuarter(instId);
            IOrderedEnumerable<ReportQuarterV1> orderedReports = reportsQuarter.Reports.OrderBy(m => m.ReportEndDate);
            foreach (ReportQuarterV1 report in orderedReports)
            {
                var fine = new FineFundamental { Symbol = symbol };
                fine.FinancialStatements.PeriodEndingDate = report.ReportEndDate ?? default;
                fine.FinancialStatements.IncomeStatement.TotalRevenue.ThreeMonths = Multiply(report.Net_Sales, Million);
                fine.FinancialStatements.IncomeStatement.OperatingRevenue.ThreeMonths = Multiply(report.Net_Sales, Million);
                fine.FinancialStatements.IncomeStatement.GrossProfit.ThreeMonths = Multiply(report.GrossIncome, Million);
                fine.FinancialStatements.IncomeStatement.OperatingIncome.ThreeMonths = Multiply(report.OperatingIncome, Million);
                fine.FinancialStatements.IncomeStatement.PretaxIncome.ThreeMonths = Multiply(report.ProfitBeforeTax, Million);
                fine.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths = Multiply(report.ProfitToEquityHolders, Million);
                fine.FinancialStatements.BalanceSheet.OtherIntangibleAssets.ThreeMonths = Multiply(report.IntangibleAssets, Million);
                fine.FinancialStatements.BalanceSheet.NetTangibleAssets.ThreeMonths = Multiply(report.TangibleAssets, Million);
                fine.FinancialStatements.BalanceSheet.FinancialAssets.ThreeMonths = Multiply(report.FinancialAssets, Million);
                fine.FinancialStatements.BalanceSheet.TotalNonCurrentAssets.ThreeMonths = Multiply(report.NonCurrentAssets, Million);
                fine.FinancialStatements.BalanceSheet.CashAndCashEquivalents.ThreeMonths = Multiply(report.CashAndEquivalents, Million);
                fine.FinancialStatements.BalanceSheet.CurrentAssets.ThreeMonths = Multiply(report.CurrentAssets, Million);
                fine.FinancialStatements.BalanceSheet.TotalAssets.ThreeMonths = Multiply(report.TotalAssets, Million);
                fine.FinancialStatements.BalanceSheet.TotalEquity.ThreeMonths = Multiply(report.TotalEquity, Million);
                fine.FinancialStatements.BalanceSheet.LongTermDebt.ThreeMonths = Multiply(report.NonCurrentLiabilities, Million);
                fine.FinancialStatements.BalanceSheet.CurrentLiabilities.ThreeMonths = Multiply(report.CurrentLiabilities, Million);
                fine.FinancialStatements.BalanceSheet.NetDebt.ThreeMonths = Multiply(report.NetDebt, Million);
                fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.ThreeMonths = Multiply(report.CashFlowFromOperatingActivities, Million);
                fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.ThreeMonths = Multiply(report.CashFlowFromInvestingActivities, Million);
                fine.FinancialStatements.CashFlowStatement.FinancingCashFlow.ThreeMonths = Multiply(report.CashFlowFromFinancingActivities, Million);
                fine.FinancialStatements.CashFlowStatement.FreeCashFlow.ThreeMonths = Multiply(report.FreeCashFlow, Million);
                fine.EarningReports.BasicEPS.ThreeMonths = Multiply(report.EarningsPerShare, 1);
                fine.EarningReports.DividendPerShare.ThreeMonths = Multiply(report.Dividend, 1);
                fine.OperationRatios.NetMargin.ThreeMonths = Divide(
                    fine.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths,
                    fine.FinancialStatements.IncomeStatement.TotalRevenue.ThreeMonths);

                DateTime lastYear = fine.FinancialStatements.PeriodEndingDate.AddDays(1).AddYears(-1).AddDays(-1);
                FineFundamental last = fundamentals.LastOrDefault(m => m.FinancialStatements.PeriodEndingDate == lastYear);
                fine.OperationRatios.RevenueGrowth.ThreeMonths = Growth(
                    fine.FinancialStatements.IncomeStatement.TotalRevenue.ThreeMonths,
                    last?.FinancialStatements.IncomeStatement.TotalRevenue.ThreeMonths);
                fine.OperationRatios.OperationIncomeGrowth.ThreeMonths = Growth(
                    fine.FinancialStatements.IncomeStatement.OperatingIncome.ThreeMonths,
                    last?.FinancialStatements.IncomeStatement.OperatingIncome.ThreeMonths);
                fine.OperationRatios.NetIncomeGrowth.ThreeMonths = Growth(
                    fine.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths,
                    last?.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths);

                UpdateFineFundamental(fundamentals, fine);
            }
        }

        private static DateTime FileDate(DateTime? reportDate, DateTime? reportEndDate)
        {
            if (reportDate != default) return (DateTime)reportDate;
            if (reportEndDate != default) return (DateTime)reportEndDate?.AddDays(45);
            throw new ArgumentNullException(nameof(reportEndDate));
        }

        private static decimal Divide(decimal a, decimal b)
        {
            if (a.Equals(decimal.MinValue)
                || b.Equals(decimal.MinValue)
                || b == 0)
            {
                return decimal.MinValue;
            }

            return a / b;
        }

        private static decimal Divide(double? a, double b)
        {
            if (a == null || b == 0) return decimal.MinValue;
            return (decimal)(a / b);
        }

        private static decimal Multiply(double? a, double b)
        {
            if (a == null) return decimal.MinValue;
            return (decimal)(a * b);
        }

        private static decimal Growth(decimal a, decimal? b)
        {
            if (a < 0) return decimal.MinValue;
            if ((b ?? 0) <= 0) return decimal.MinValue;
            return (decimal)((a - b) / b);
        }

        private static FineFundamental ReadFineFundamentalFile(string zipPath, Symbol symbol)
        {
            string jsonFile = $"{symbol.Value.ToLowerInvariant()}.json";
            using StreamReader resultStream = Compression.Unzip(zipPath, jsonFile, out ZipFile zFile);
            Debug.Assert(resultStream != null);
            using (zFile)
            {
                using JsonReader reader = new JsonTextReader(resultStream);
                JsonSerializer serializer = new();
                FineFundamental fine = serializer.Deserialize<FineFundamental>(reader);
                return fine;
            }
        }

        private static void WriteFineFundamentalFiles(IList<FineFundamental> fundamentals, Symbol symbol, string folder)
        {
            // Remove all files in folder
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            // Write back all fundamentals to files
            Directory.CreateDirectory(folder);
            foreach (FineFundamental fine in fundamentals)
            {
                // Skip Quarter only reports
                if (string.IsNullOrEmpty(fine.FinancialStatements.PeriodType)) continue;

                string zipFile = string.Format(CultureInfo.InvariantCulture, "{0:yyyyMMdd}.zip", fine.FinancialStatements.FileDate);
                string zipPath = Path.Combine(folder, zipFile);
                string jsonFile = $"{symbol.Value.ToLowerInvariant()}.json";
                string jsonString = JsonConvert.SerializeObject(fine);
                Compression.ZipData(zipPath, new Dictionary<string, string> { { jsonFile, jsonString } });
            }
        }

        private static void UpdateFineFundamental(IList<FineFundamental> fundamentals, FineFundamental fine)
        {
            FineFundamental updated = fundamentals.FirstOrDefault(m => m.FinancialStatements.PeriodEndingDate.Equals(fine.FinancialStatements.PeriodEndingDate));
            if (updated == default)
            {
                fundamentals.Add(fine);
            }
            else
            {
                updated.UpdateValues(fine);
            }
        }

        private IEnumerable<BaseData> DownloadSymbol(
            Symbol symbol,
            long instId,
            Resolution resolution,
            DateTime afterUtc,
            DateTime endUtc)
        {
            endUtc = endUtc.Date;
            StockPricesRespV1 stockPrices;
            string zipPath = Path.Combine(
                Globals.DataFolder,
                symbol.ID.SecurityType.SecurityTypeToLower(),
                symbol.ID.Market,
                resolution.ResolutionToLower(),
                symbol.Value + ".zip");

            if (File.Exists(zipPath))
            {
                DateTime fromDate = afterUtc.Date.AddDays(1);
                if (fromDate > endUtc) return default;

                // Reload data on split
                if (_splits.stockSplitList.Any(m => m.InstrumentId.Equals(instId) && m.SplitDate > afterUtc))
                {
                    File.Delete(zipPath);
                    stockPrices = GetStockPrices(instId, FirstDate, endUtc);
                }
                else
                {
                    stockPrices = GetStockPrices(instId, fromDate, endUtc);
                    if (stockPrices.StockPricesList.Count == 0)
                    {
                        return default;
                    }
                }
            }
            else
            {
                stockPrices = GetStockPrices(instId, FirstDate, endUtc);
            }

            return ToTradeBars(symbol, stockPrices);
        }

        private StockPricesRespV1 GetStockPrices(long instId, DateTime fromDate, DateTime toDate)
        {
            //Log.Trace("{0}: Download prices {1} {2} {3}",
            //    GetType().Name,
            //    symbol.Value,
            //    fromDate.ToShortDateString(),
            //    toDate.Date.ToShortDateString());

            StockPricesRespV1 stockPrices;
            var stockPriceList = new List<StockPriceV1>();
            while (fromDate <= toDate)
            {
                DateTime lastDate = fromDate.AddYears(10).AddDays(-1);
                if (lastDate > toDate)
                {
                    lastDate = toDate;
                }

                StockPricesRespV1 sp = _api.GetStockPrices(instId, fromDate, lastDate);
                stockPriceList.AddRange(sp.StockPricesList);
                fromDate = lastDate.AddDays(1);
            }

            stockPrices = new StockPricesRespV1 { StockPricesList = stockPriceList };
            return stockPrices;
        }

        private IEnumerable<BaseData> ToTradeBars(Symbol symbol, StockPricesRespV1 stockPrices)
        {
            var bars = new List<TradeBar>();
            foreach (StockPriceV1 price in stockPrices.StockPricesList)
            {
                //                    Debug.WriteLine($"{bar.D} {bar.O} {bar.H} {bar.L} {bar.C} {bar.V}");
                double close = price.C ?? 0;
                var bar = new TradeBar
                {
                    Time = DateTime.Parse(price.D, CultureInfo.InvariantCulture),
                    Open = (decimal)(price.O ?? close),
                    High = (decimal)(price.H ?? close),
                    Low = (decimal)(price.L ?? close),
                    Close = (decimal)close,
                    Volume = price.V ?? 0
                };

                bool barOk = bar.Low > 0 && bar.Low <= bar.Open && bar.Low <= bar.High && bar.Low <= bar.Close && bar.Open <= bar.High && bar.Close <= bar.High;
                if (!barOk)
                {
                    Log.Trace($"{GetType().Name}: Invalid OHLC: {symbol.Value} {bar.Time:d} {bar.Open} {bar.High} {bar.Low} {bar.Close}");
                    continue;
                }

                // Check for updates
                TradeBar item = bars.Find(m => m.Time.Equals(bar.Time));
                if (item != null)
                {
                    item.Open = bar.Open;
                    item.High = bar.High;
                    item.Low = bar.Low;
                    item.Close = bar.Close;
                    item.Volume = bar.Volume;
                }
                else
                {
                    bars.Add(bar);
                }
            }

            return bars;
        }
    }
}
