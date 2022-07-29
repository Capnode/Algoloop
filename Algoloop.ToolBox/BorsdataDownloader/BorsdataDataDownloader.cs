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
using QuantConnect.Configuration;
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
        private const string _annual = "Annual";
        private const string _quarter = "Quarter";
        private const double _million = 1e6;
        private const int _reportDateLatest = 202;
        private static readonly DateTime _firstDate = new DateTime(1997, 01, 01);

        private KpisAllCompRespV1 _lastReports;
        private bool _isDisposed;
        private readonly ApiClient _api;
        private InstrumentRespV1 _allInstruments;
        private StockSplitRespV1 _splits;

        public BorsdataDataDownloader(string apiKey)
        {
            _api = new ApiClient(apiKey);
        }

        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime afterUtc, DateTime endUtc)
        {
            //            Log.Trace($"{GetType().Name}: Get {symbol.Value} {resolution} {startUtc.ToShortDateString()} {startUtc.ToShortTimeString()}");
            Initialize();
            InstrumentV1 inst = _allInstruments.Instruments.Find(m => m.Yahoo.Equals(symbol.Value));
            if (inst == default) return null;

            // Reload preliminary data
            TimeZoneInfo cetZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            DateTime afterCet = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, cetZone);
            DateTime after = afterCet.Hour < 21 ? afterCet.AddDays(-1).Date : afterCet.Date;

            // Download reports
            DownloadReports(symbol, inst.InsId.Value, after);

            // Download price data
            return DownloadSymbol(symbol, inst.InsId.Value, resolution, after, endUtc);
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

        public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                if (_api != null)
                {
                    _api.Dispose();
                }
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
                _lastReports = _api.GetKpiScreener(_reportDateLatest, TimeType.last, CalcType.latest);
            }
        }

        private static SymbolModel CreateSymbolModel(InstrumentV1 inst)
        {
            if (string.IsNullOrWhiteSpace(inst.Yahoo) || inst.Yahoo.Contains(" ")) return null;

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
            // Create FineFundamentals folder
            string folder = Path.Combine(
                Globals.DataFolder,
                symbol.ID.SecurityType.SecurityTypeToLower(),
                symbol.ID.Market,
                "fundamental",
                "fine",
                symbol.Value.ToLowerInvariant());

            // Check if directory exists
            if (!Directory.Exists(folder))
            {
                // Create folder
                Directory.CreateDirectory(folder);
                Log.Trace($"{GetType().Name}: Update report {symbol.Value}");
            }
            else
            {
                // Remove reports after afterUtc
                DirectoryInfo d = new DirectoryInfo(folder);
                foreach (FileInfo zipFile in d.GetFiles("*.zip"))
                {
                    string fileName = Path.GetFileNameWithoutExtension(zipFile.Name);
                    DateTime fileDate = DateTime.ParseExact(
                        fileName,
                        "yyyyMMdd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None);

                    if (fileDate > afterUtc)
                    {
                        zipFile.Delete();
                    }
                }

                // Check any report after afterUtc
                KpiV1 report = _lastReports.Values.Where(m =>
                    m.I.Equals(instId) &&
                    !string.IsNullOrEmpty(m.S) &&
                    DateTime.Parse(m.S, CultureInfo.InvariantCulture) > afterUtc).SingleOrDefault();
                if (report == default) return;
                DateTime date = DateTime.Parse(report.S, CultureInfo.InvariantCulture);
                Log.Trace($"{GetType().Name}: {date:d} Update report {symbol.Value}");
            }

            // Download reports
            ReportsYearRespV1 reportsYear = _api.GetReportsYear(instId);
            ReportsR12RespV1 reportsR12 = _api.GetReportsR12(instId);
            ReportsQuarterRespV1 reportsQuarter = _api.GetReportsQuarter(instId);

            // Update reports
            ReportsYear(symbol, folder, reportsYear.Reports);
            ReportsR12(symbol, folder, reportsR12.Reports);
            ReportsQuarter(symbol, folder, reportsQuarter.Reports);
        }

        private static void ReportsYear(Symbol symbol, string folder, List<ReportYearV1> reports)
        {
            FineFundamental last = null;
            IOrderedEnumerable<ReportYearV1> orderedReports = reports.OrderBy(m => m.ReportEndDate);
            foreach (ReportYearV1 report in orderedReports)
            {
                var fine = new FineFundamental { Symbol = symbol };
                fine.FinancialStatements.PeriodType = _annual;
                DateTime endDate = report.ReportEndDate ?? default;
                fine.FinancialStatements.PeriodEndingDate = endDate;
                fine.FinancialStatements.FileDate = FileDate(report.ReportDate, report.ReportEndDate);
                fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths = Multiply(
                    report.Revenues > 0 ? report.Revenues : default,
                    _million);
                fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths = Multiply(report.ProfitToEquityHolders, _million);
                fine.OperationRatios.NetMargin.OneYear = Divide(
                    fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths,
                    fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths);
                fine.FinancialStatements.CashFlowStatement.FinancingCashFlow.TwelveMonths = Multiply(report.CashFlowFromFinancingActivities, _million);
                fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.TwelveMonths = Multiply(report.CashFlowFromInvestingActivities, _million);
                fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths = Multiply(report.CashFlowFromOperatingActivities, _million);
                fine.EarningReports.DividendPerShare.TwelveMonths = Multiply(report.Dividend, 1);

                fine.CompanyProfile.SharesOutstanding = (long)((report.NumberOfShares ?? 0) * _million);
                if (fine.CompanyProfile.SharesOutstanding > 0)
                {
                    double earningsPerShare = (double)fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths / fine.CompanyProfile.SharesOutstanding;
                    fine.ValuationRatios.PERatio = Divide(report.StockPriceAverage, earningsPerShare);
                    double salsePerShare = (double)fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths / fine.CompanyProfile.SharesOutstanding;
                    fine.ValuationRatios.PSRatio = Divide(report.StockPriceAverage, salsePerShare);
                }

                if (last != default)
                {
                    fine.OperationRatios.RevenueGrowth.OneYear = Growth(
                        fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths,
                        last.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths);
                    fine.OperationRatios.NetIncomeGrowth.OneYear = Growth(
                        fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths,
                        last.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths);
                    WriteFineFundamentalFile(symbol, folder, fine);
                }

                last = fine;
            }
        }

        private static void ReportsR12(Symbol symbol, string folder, List<ReportR12V1> reports)
        {
            var fundamentals = new List<FineFundamental>();
            ReportR12V1[] orderedReports = reports.OrderBy(m => m.ReportEndDate).ToArray();
            foreach (ReportR12V1 report in orderedReports)
            {
                var fine = new FineFundamental { Symbol = symbol };
                fine.FinancialStatements.PeriodType = _quarter;
                DateTime endDate = report.ReportEndDate ?? default;
                fine.FinancialStatements.PeriodEndingDate = endDate;
                fine.FinancialStatements.FileDate = FileDate(report.ReportDate, report.ReportEndDate);
                fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths = Multiply(
                    report.Revenues > 0 ? report.Revenues : default,
                    _million);
                fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths = Multiply(report.ProfitToEquityHolders, _million);
                fine.OperationRatios.NetMargin.OneYear = Divide(
                    fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths,
                    fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths);
                fine.FinancialStatements.CashFlowStatement.FinancingCashFlow.TwelveMonths = Multiply(report.CashFlowFromFinancingActivities, _million);
                fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.TwelveMonths = Multiply(report.CashFlowFromInvestingActivities, _million);
                fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths = Multiply(report.CashFlowFromOperatingActivities, _million);
                fine.EarningReports.DividendPerShare.TwelveMonths = Multiply(report.Dividend, 1);

                fine.CompanyProfile.SharesOutstanding = (long)((report.NumberOfShares ?? 0) * _million);
                if (fine.CompanyProfile.SharesOutstanding > 0)
                {
                    double earningsPerShare = (double)fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths / fine.CompanyProfile.SharesOutstanding;
                    fine.ValuationRatios.PERatio = Divide(report.StockPriceAverage, earningsPerShare);
                    double salsePerShare = (double)fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths / fine.CompanyProfile.SharesOutstanding;
                    fine.ValuationRatios.PSRatio = Divide(report.StockPriceAverage, salsePerShare);
                }

                // Find report, at least one year old
                DateTime nextMonth = fine.FinancialStatements.PeriodEndingDate.AddMonths(1);
                DateTime before = new DateTime(nextMonth.Year, nextMonth.Month, 1, 0, 0, 0, nextMonth.Kind).AddYears(-1);
                FineFundamental last = fundamentals.LastOrDefault(m => m.FinancialStatements.PeriodEndingDate < before);
                if (last != default)
                {
                    fine.OperationRatios.RevenueGrowth.OneYear = Growth(
                        fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths,
                        last.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths);
                    fine.OperationRatios.NetIncomeGrowth.OneYear = Growth(
                        fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths,
                        last.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths);
                    WriteFineFundamentalFile(symbol, folder, fine);
                }

                fundamentals.Add(fine);
            }
        }

        private static void ReportsQuarter(Symbol symbol, string folder, List<ReportQuarterV1> reports)
        {
            FineFundamental last = null;
            IOrderedEnumerable<ReportQuarterV1> orderedReports = reports.OrderBy(m => m.ReportEndDate);
            foreach (ReportQuarterV1 report in orderedReports)
            {
                var fine = new FineFundamental { Symbol = symbol };
                DateTime endDate = report.ReportEndDate ?? default;
                fine.FinancialStatements.PeriodEndingDate = endDate;
                fine.FinancialStatements.FileDate = FileDate(report.ReportDate, report.ReportEndDate);
                fine.FinancialStatements.IncomeStatement.TotalRevenue.ThreeMonths = Multiply(
                    report.Revenues > 0 ? report.Revenues : default,
                    _million);
                fine.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths = Multiply(report.ProfitToEquityHolders, _million);
                fine.OperationRatios.NetMargin.ThreeMonths = Divide(
                    fine.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths,
                    fine.FinancialStatements.IncomeStatement.TotalRevenue.ThreeMonths);
                fine.FinancialStatements.CashFlowStatement.FinancingCashFlow.ThreeMonths = Multiply(report.CashFlowFromFinancingActivities, _million);
                fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.ThreeMonths = Multiply(report.CashFlowFromInvestingActivities, _million);
                fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.ThreeMonths = Multiply(report.CashFlowFromOperatingActivities, _million);
                fine.EarningReports.DividendPerShare.ThreeMonths = Multiply(report.Dividend, 1);
                fine.CompanyProfile.SharesOutstanding = (long)((report.NumberOfShares ?? 0) * _million);

                if (last != default)
                {
                    fine.OperationRatios.RevenueGrowth.ThreeMonths = Growth(
                        fine.FinancialStatements.IncomeStatement.TotalRevenue.ThreeMonths,
                        last.FinancialStatements.IncomeStatement.TotalRevenue.ThreeMonths);
                    fine.OperationRatios.NetIncomeGrowth.ThreeMonths = Growth(
                        fine.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths,
                        last.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths);
                    WriteFineFundamentalFile(symbol, folder, fine);
                }

                last = fine;
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
            if (a == default || b == 0) return decimal.MinValue;
            return (decimal)(a / b);
        }

        private static decimal Multiply(double? a, double b)
        {
            if (a == default) return decimal.MinValue;
            return (decimal)(a * b);
        }

        private static decimal Growth(decimal a, decimal b)
        {
            if (a < 0) return decimal.MinValue;
            if (b <= 0) return 0;
            return (a - b) / b;
        }

        private static void WriteFineFundamentalFile(Symbol symbol, string folder, FineFundamental fine)
        {
            string zipFile = string.Format(CultureInfo.InvariantCulture, "{0:yyyyMMdd}.zip", fine.FinancialStatements.FileDate);
            string zipPath = Path.Combine(folder, zipFile);
            string jsonFile = $"{symbol.Value.ToLowerInvariant()}.json";
            if (File.Exists(zipPath))
            {
                // Do not update some values
                fine.FinancialStatements.PeriodType = null;

                using (StreamReader resultStream = Compression.Unzip(zipPath, jsonFile, out ZipFile zFile))
                {
                    Debug.Assert(resultStream != null);
                    Debug.Assert(zipFile != null);
                    using (zFile)
                    {
                        using (JsonReader reader = new JsonTextReader(resultStream))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            FineFundamental updated = serializer.Deserialize<FineFundamental>(reader);
                            Debug.Assert(updated != null);
                            updated.CompanyProfile.UpdateValues(fine.CompanyProfile);
                            updated.FinancialStatements.UpdateValues(fine.FinancialStatements);
                            updated.OperationRatios.UpdateValues(fine.OperationRatios);
                            updated.ValuationRatios.UpdateValues(fine.ValuationRatios);
                            fine = updated;
                        }
                    }

                    File.Delete(zipPath);
                }
            }

            // Skip if Quarter report only
            if (fine.FinancialStatements.PeriodType == default) return;

            string jsonString = JsonConvert.SerializeObject(fine);
            Compression.ZipData(zipPath, new Dictionary<string, string> { { jsonFile, jsonString } });
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
                    stockPrices = GetStockPrices(instId, symbol, _firstDate, endUtc);
                }
                else
                {
                    stockPrices = GetStockPrices(instId, symbol, fromDate, endUtc);
                    if (stockPrices.StockPricesList.Count == 0)
                    {
                        Log.Trace($"{GetType().Name}: Prices not found: {symbol} {fromDate:d} to {endUtc:d}");
                        return default;
                    }
                }
            }
            else
            {
                stockPrices = GetStockPrices(instId, symbol, _firstDate, endUtc);
            }

            return ToTradeBars(symbol, stockPrices);
        }

        private StockPricesRespV1 GetStockPrices(long instId, Symbol symbol, DateTime fromDate, DateTime toDate)
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
