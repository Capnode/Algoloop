/*
 * Copyright 2019 Capnode AB
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

using Ionic.Zip;
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using System.Diagnostics;
using System.Globalization;

namespace Algoloop.Algorithm.CSharp
{
    internal class FundamentalSignal : ISignal
    {
        private const decimal Million = 1e6m;
        
        private readonly Symbol _symbol;
        private readonly decimal? _marketCap;
        private readonly decimal? _netIncome;
        private readonly decimal? _netIncomeQuarter;
        private readonly decimal? _netIncomeGrowth;
        private readonly decimal? _netIncomeInverseGrowth;
        private readonly decimal? _netIncomeTrend;
        private readonly decimal? _revenueGrowth;
        private readonly decimal? _revenueInverseGrowth;
        private readonly decimal? _revenueTrend;
        private readonly decimal? _netMargin;
        private readonly decimal? _netMarginTrend;
        private readonly decimal? _freeCashFlowMargin;
        private readonly decimal? _freeCashFlowMarginTrend;
        private readonly decimal? _returnOnEquity;
        private readonly decimal? _peRatio;
        private readonly decimal? _epRatio;
        private readonly decimal? _psRatio;
        private readonly decimal? _spRatio;
        private readonly List<FineFundamental> _fineFundamentals = new();

        public FundamentalSignal(
            QCAlgorithm algorithm,
            Symbol symbol,
            string marketCap = null,
            string netIncome = null,
            string netIncomeQuarter = null,
            string netIncomeGrowth = null,
            string netIncomeInverseGrowth = null,
            string netIncomeTrend = null,
            string revenueGrowth = null,
            string revenueInverseGrowth = null,
            string revenueTrend = null,
            string netMargin = null,
            string netMarginTrend = null,
            string freeCashFlowMargin = null,
            string freeCashFlowMarginTrend = null,
            string returnOnEquity = null,
            string peRatio = null,
            string epRatio = null,
            string psRatio = null,
            string spRatio = null)
        {
            _symbol = symbol;
            if (!string.IsNullOrWhiteSpace(marketCap))
            {
                if (decimal.TryParse(marketCap, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal value))
                {
                    _marketCap = value * Million;
                }
                else if (marketCap.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _marketCap = decimal.MinValue;
                }
                else if (!marketCap.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(marketCap)}' has invalid value: {marketCap}");
                }
            }

            if (!string.IsNullOrWhiteSpace(netIncome))
            {
                if (decimal.TryParse(netIncome, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal value))
                {
                    _netIncome = value;
                }
                else if (netIncome.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _netIncome = decimal.MinValue;
                }
                else if (!netIncome.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(netIncome)}' has invalid value: {netIncome}");
                }
            }

            if (!string.IsNullOrWhiteSpace(netIncomeQuarter))
            {
                if (decimal.TryParse(netIncomeQuarter, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal value))
                {
                    _netIncomeQuarter = value;
                }
                else if (netIncomeQuarter.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _netIncomeQuarter = decimal.MinValue;
                }
                else if (!netIncomeQuarter.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(netIncomeQuarter)}' has invalid value: {netIncomeQuarter}");
                }
            }

            if (!string.IsNullOrWhiteSpace(netIncomeGrowth))
            {
                if (decimal.TryParse(netIncomeGrowth, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _netIncomeGrowth = value / 100;
                }
                else if (netIncomeGrowth.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _netIncomeGrowth = decimal.MinValue;
                }
                else if (!netIncomeGrowth.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(netIncomeGrowth)}' has invalid value: {netIncomeGrowth}");
                }
            }

            if (!string.IsNullOrWhiteSpace(netIncomeInverseGrowth))
            {
                if (decimal.TryParse(netIncomeInverseGrowth, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _netIncomeInverseGrowth = value / 100;
                }
                else if (netIncomeInverseGrowth.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _netIncomeInverseGrowth = decimal.MinValue;
                }
                else if (!netIncomeInverseGrowth.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(netIncomeInverseGrowth)}' has invalid value: {netIncomeInverseGrowth}");
                }
            }

            if (!string.IsNullOrWhiteSpace(netIncomeTrend))
            {
                if (decimal.TryParse(netIncomeTrend, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _netIncomeTrend = value / 100;
                }
                else if (netIncomeTrend.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _netIncomeTrend = decimal.MinValue;
                }
                else if (!netIncomeTrend.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(netIncomeTrend)}' has invalid value: {netIncomeTrend}");
                }
            }

            if (!string.IsNullOrWhiteSpace(revenueGrowth))
            {
                if (decimal.TryParse(revenueGrowth, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _revenueGrowth = value / 100;
                }
                else if (revenueGrowth.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _revenueGrowth = decimal.MinValue;
                }
                else if (!revenueGrowth.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(revenueGrowth)}' has invalid value: {revenueGrowth}");
                }
            }

            if (!string.IsNullOrWhiteSpace(revenueInverseGrowth))
            {
                if (decimal.TryParse(revenueInverseGrowth, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _revenueInverseGrowth = value / 100;
                }
                else if (revenueInverseGrowth.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _revenueInverseGrowth = decimal.MinValue;
                }
                else if (!revenueInverseGrowth.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(revenueInverseGrowth)}' has invalid value: {revenueInverseGrowth}");
                }
            }

            if (!string.IsNullOrWhiteSpace(revenueTrend))
            {
                if (int.TryParse(revenueTrend, NumberStyles.Number, CultureInfo.InvariantCulture, out int value))
                {
                    _revenueTrend = value / 100;
                }
                else if (revenueTrend.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _revenueTrend = decimal.MinValue;
                }
                else if (!revenueTrend.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(revenueTrend)}' has invalid value: {revenueTrend}");
                }
            }

            if (!string.IsNullOrWhiteSpace(netMargin))
            {
                if (decimal.TryParse(netMargin, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _netMargin = value / 100;
                }
                else if (netMargin.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _netMargin = decimal.MinValue;
                }
                else if (!netMargin.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(netMargin)}' has invalid value: {netMargin}");
                }
            }

            if (!string.IsNullOrWhiteSpace(netMarginTrend))
            {
                if (decimal.TryParse(netMarginTrend, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _netMarginTrend = value / 100;
                }
                else if (netMarginTrend.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _netMarginTrend = decimal.MinValue;
                }
                else if (!netMarginTrend.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(netMarginTrend)}' has invalid value: {netMarginTrend}");
                }
            }

            if (!string.IsNullOrWhiteSpace(freeCashFlowMargin))
            {
                if (decimal.TryParse(freeCashFlowMargin,  NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _freeCashFlowMargin =  value / 100;
                }
                else if (freeCashFlowMargin.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _freeCashFlowMargin = decimal.MinValue;
                }
                else if (!freeCashFlowMargin.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(freeCashFlowMargin)}' has invalid value: {freeCashFlowMargin}");
                }
            }

            if (!string.IsNullOrWhiteSpace(freeCashFlowMarginTrend))
            {
                if (decimal.TryParse(freeCashFlowMarginTrend, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _freeCashFlowMarginTrend = value / 100;
                }
                else if (freeCashFlowMarginTrend.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _freeCashFlowMarginTrend = decimal.MinValue;
                }
                else if (!freeCashFlowMarginTrend.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(freeCashFlowMarginTrend)}' has invalid value: {freeCashFlowMarginTrend}");
                }
            }

            if (!string.IsNullOrWhiteSpace(returnOnEquity))
            {
                if (decimal.TryParse(returnOnEquity, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _returnOnEquity = value / 100;
                }
                else if (returnOnEquity.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _returnOnEquity = decimal.MinValue;
                }
                else if (!returnOnEquity.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(returnOnEquity)}' has invalid value: {returnOnEquity}");
                }
            }

            if (!string.IsNullOrWhiteSpace(peRatio))
            {
                if (decimal.TryParse(peRatio, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _peRatio = value;
                }
                else if (peRatio.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _peRatio = decimal.MinValue;
                }
                else if (!peRatio.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(peRatio)}' has invalid value: {peRatio}");
                }
            }

            if (!string.IsNullOrWhiteSpace(epRatio))
            {
                if (decimal.TryParse(epRatio, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _epRatio = value;
                }
                else if (epRatio.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _epRatio = decimal.MinValue;
                }
                else if (!epRatio.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(epRatio)}' has invalid value: {epRatio}");
                }
            }

            if (!string.IsNullOrWhiteSpace(psRatio))
            {
                if (decimal.TryParse(psRatio, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _psRatio = value;
                }
                else if (psRatio.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _psRatio = decimal.MinValue;
                }
                else if (!psRatio.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(psRatio)}' has invalid value: {psRatio}");
                }
            }

            if (!string.IsNullOrWhiteSpace(spRatio))
            {
                if (decimal.TryParse(spRatio, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    _spRatio = value;
                }
                else if (spRatio.Equals("USE", StringComparison.OrdinalIgnoreCase))
                {
                    _spRatio = decimal.MinValue;
                }
                else if (!spRatio.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm.Log($"{symbol} parameter '{nameof(spRatio)}' has invalid value: {spRatio}");
                }
            }
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            ReadFineFundamental(bar.Time);
            return EvaluateFundamentals(bar.Price);
        }

        private void ReadFineFundamental(DateTime date)
        {
            string folder = Path.Combine(
                Globals.DataFolder,
                SecurityType.Equity.ToString(),
                _symbol.ID.Market,
                "fundamental",
                "fine",
                _symbol.Value.ToLowerInvariant());

            DirectoryInfo d = new(folder);
            if (!d.Exists) return;

            foreach (FileInfo zipFile in d.GetFiles("*.zip"))
            {
                string fileName = Path.GetFileNameWithoutExtension(zipFile.Name);
                DateTime fileDate = DateTime.ParseExact(
                    fileName,
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None);

                if (fileDate > date
                    || _fineFundamentals.Any(m => m.FinancialStatements.FileDate.Equals(fileDate)))
                {
                    continue;
                }

                // Read fundamental file
                string jsonFile = $"{_symbol.Value.ToLowerInvariant()}.json";
                using (StreamReader resultStream = Compression.Unzip(zipFile.FullName, jsonFile, out ZipFile file))
                using (file)
                {
                    if (resultStream == null) return;
                    using JsonReader reader = new JsonTextReader(resultStream);
                    JsonSerializer serializer = new();
                    FineFundamental fine = serializer.Deserialize<FineFundamental>(reader);

                    // Assert no duplicates
                    Debug.Assert(!_fineFundamentals.Any(m => m.FinancialStatements.FileDate.Date.Equals(fine.FinancialStatements.FileDate.Date)));

                    // Decending sort
                    _fineFundamentals.Add(fine);
                    _fineFundamentals.Sort((x, y) => -DateTime.Compare(x.FinancialStatements.FileDate, y.FinancialStatements.FileDate));
                }
            }
        }

        private float EvaluateFundamentals(decimal price)
        {
            int count = _fineFundamentals.Count;
            if (count == 0) return 0;

            FineFundamental fine = _fineFundamentals[0];
            FineFundamental prev = count > 1 ? _fineFundamentals[1] : null;

            // NetIncomeGrowth
            decimal netIncomeGrowth = fine.OperationRatios.NetIncomeGrowth.OneYear;
            decimal netIncomeInverseGrowth = netIncomeGrowth == 0 ? 0 : 1 / netIncomeGrowth;

            // RevenueGrowth
            decimal revenueGrowth = fine.OperationRatios.RevenueGrowth.OneYear;
            decimal revenueInverseGrowth = (revenueGrowth == 0) ? 0 : 1 / revenueGrowth;

            // Total company value
            decimal marketCap = price * fine.CompanyProfile.SharesOutstanding;

            // Update PE ratio
            decimal earningsPerShare = fine.CompanyProfile.SharesOutstanding == 0 ? decimal.MinValue :
                fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths / fine.CompanyProfile.SharesOutstanding;
            fine.ValuationRatios.PERatio = earningsPerShare == 0 ? decimal.MinValue : price / earningsPerShare;
            decimal epRatio = fine.ValuationRatios.PERatio == 0 ? decimal.MinValue : 1 / fine.ValuationRatios.PERatio;

            // Update PS ratio
            decimal salesPerShare = fine.CompanyProfile.SharesOutstanding == 0 ? decimal.MinValue :
                fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths / fine.CompanyProfile.SharesOutstanding;
            fine.ValuationRatios.PSRatio = salesPerShare == 0 ? decimal.MinValue : price / salesPerShare;
            decimal spRatio = fine.ValuationRatios.PSRatio == 0 ? decimal.MinValue : 1 / fine.ValuationRatios.PSRatio;
            decimal freeCashFlowMargin = FreeCashFlowMargin(fine);

            // Calculate net income growth trend
            // Calculate revenue growth trend
            // Calculate net margin growth
            decimal netIncomeTrend = 0;
            decimal revenueTrend = 0;
            decimal netMarginTrend = 0;
            decimal freeCashFlowMarginTrend = 0;
            if (prev != null)
            {
                netIncomeTrend = Trend(fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths,
                    prev.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths);
                revenueTrend = Trend(fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths,
                    prev.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths);
                netMarginTrend = Trend(fine.OperationRatios.NetMargin.OneYear,
                    prev.OperationRatios.NetMargin.OneYear);
                freeCashFlowMarginTrend = Trend(freeCashFlowMargin, FreeCashFlowMargin(prev));
            }

            // Calculate return on equity
            decimal netIncome = fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths;
            decimal equity = fine.FinancialStatements.BalanceSheet.TotalEquity.TwelveMonths;
            decimal returnOnEquity = equity > 0 ? netIncome / equity : decimal.MinValue;

            //            _algorithm.Log($"netMargin {fine.OperationRatios.NetMargin.OneYear} {netMarginTrend} freeCashFlowMargin {freeCashFlowMargin} {freeCashFlowMarginTrend}");
            // Check limits
            if (marketCap < (_marketCap ?? decimal.MinValue)
            || fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths < (_netIncome ?? decimal.MinValue)
            || fine.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths < (_netIncomeQuarter ?? decimal.MinValue)
            || netIncomeGrowth < (_netIncomeGrowth ?? decimal.MinValue)
            || netIncomeInverseGrowth < (_netIncomeInverseGrowth ?? decimal.MinValue)
            || netIncomeTrend < (_netIncomeTrend ?? decimal.MinValue)
            || revenueGrowth < (_revenueGrowth ?? decimal.MinValue)
            || revenueInverseGrowth < (_revenueInverseGrowth ?? decimal.MinValue)
            || revenueTrend < (_revenueTrend ?? decimal.MinValue)
            || fine.OperationRatios.NetMargin.OneYear < (_netMargin ?? decimal.MinValue)
            || netMarginTrend < (_netMarginTrend ?? decimal.MinValue)
            || freeCashFlowMargin < (_freeCashFlowMargin ?? decimal.MinValue)
            || freeCashFlowMarginTrend < (_freeCashFlowMarginTrend ?? decimal.MinValue)
            || returnOnEquity < (_returnOnEquity ?? decimal.MinValue)
            || fine.ValuationRatios.PERatio < (_peRatio ?? decimal.MinValue)
            || epRatio < (_epRatio ?? decimal.MinValue)
            || fine.ValuationRatios.PSRatio < (_psRatio ?? decimal.MinValue)
            || spRatio < (_spRatio ?? decimal.MinValue))
            {
                return 0;
            }

            // Aggregate values in score calculations
            float score = float.NaN; // NaN means pass filter
            if (_marketCap.Equals(decimal.MinValue))
            {
                score = Aggregate(score, marketCap);
            }

            if (_netIncome.Equals(decimal.MinValue))
            {
                score = Aggregate(score, fine.FinancialStatements.IncomeStatement.NetIncome.TwelveMonths);
            }

            if (_netIncomeQuarter.Equals(decimal.MinValue))
            {
                score = Aggregate(score, fine.FinancialStatements.IncomeStatement.NetIncome.ThreeMonths);
            }

            if (_netIncomeGrowth.Equals(decimal.MinValue))
            {
                score = Aggregate(score, netIncomeGrowth);
            }

            if (_netIncomeInverseGrowth.Equals(decimal.MinValue))
            {
                score = Aggregate(score, netIncomeInverseGrowth);
            }

            if (_netIncomeTrend.Equals(decimal.MinValue))
            {
                score = Aggregate(score, netIncomeTrend);
            }

            if (_revenueGrowth.Equals(decimal.MinValue))
            {
                score = Aggregate(score, revenueGrowth);
            }

            if (_revenueInverseGrowth.Equals(decimal.MinValue))
            {
                score = Aggregate(score, revenueInverseGrowth);
            }

            if (_revenueTrend.Equals(decimal.MinValue))
            {
                score = Aggregate(score, revenueTrend);
            }

            if (_netMargin.Equals(decimal.MinValue))
            {
                score = Aggregate(score, fine.OperationRatios.NetMargin.OneYear);
            }

            if (_netMarginTrend.Equals(decimal.MinValue))
            {
                score = Aggregate(score, netMarginTrend);
            }

            if (_freeCashFlowMargin.Equals(decimal.MinValue))
            {
                score = Aggregate(score, freeCashFlowMargin);
            }

            if (_freeCashFlowMarginTrend.Equals(decimal.MinValue))
            {
                score = Aggregate(score, freeCashFlowMarginTrend);
            }

            if (_returnOnEquity.Equals(decimal.MinValue))
            {
                score = Aggregate(score, returnOnEquity);
            }

            if (_peRatio.Equals(decimal.MinValue))
            {
                score = Aggregate(score, fine.ValuationRatios.PERatio);
            }

            if (_epRatio.Equals(decimal.MinValue))
            {
                score = Aggregate(score, epRatio);
            }

            if (_psRatio.Equals(decimal.MinValue))
            {
                score = Aggregate(score, fine.ValuationRatios.PSRatio);
            }

            if (_spRatio.Equals(decimal.MinValue))
            {
                score = Aggregate(score, spRatio);
            }

            return score;
        }

        private static decimal FreeCashFlowMargin(FineFundamental fine)
        {
            if (fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths == 0
                || fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths.Equals(decimal.MinValue)
                || fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.TwelveMonths.Equals(decimal.MinValue))
            {
                return decimal.MinValue;
            }

            return (fine.FinancialStatements.CashFlowStatement.OperatingCashFlow.TwelveMonths
                + fine.FinancialStatements.CashFlowStatement.InvestingCashFlow.TwelveMonths)
                / fine.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths;
        }

        private static decimal Trend(decimal actual, decimal previous)
        {
            if (actual < 0) return decimal.MinValue;
            if (previous <= 0) return 0;
            return (actual - previous) / previous;
        }

        private static float Aggregate(float score, decimal value)
        {
            if (score.Equals(float.NaN)) return (float)value;
            if (score > 0 && value > 0) return score * (float)value;
            if (score < 0 && value < 0) return -score * (float)value;
            return 0;
        }

        public void Done()
        {
        }
    }
}
