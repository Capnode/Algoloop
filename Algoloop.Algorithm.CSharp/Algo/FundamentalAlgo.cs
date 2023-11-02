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

using Algoloop.Algorithm.CSharp.Model;
using Algoloop.Algorithm.CSharp.Signal;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders.Fees;
using QuantConnect.Parameters;
using QuantConnect.Securities;
using System.Globalization;

namespace Algoloop.Algorithm.CSharp.Algo
{
    public class FundamentalAlgo : QCAlgorithm
    {
        [Parameter("symbols")]
        private readonly string _symbols = null;

        [Parameter("security")]
        private readonly string _security = null;

        [Parameter("resolution")]
        private readonly string _resolution = null;

        [Parameter("market")]
        private readonly string _market = null;

        [Parameter("Fee")]
        private readonly string _fee = "0";

        [Parameter("Slots")]
        private readonly string _slots = "1";

        [Parameter("Reinvest")]
        private readonly string _reinvest = "false";

        [Parameter("Rebalance trigger (min)")]
        private readonly string _rebalance = "0";

        [Parameter("Tracker sma stoploss period")]
        private readonly string _smaPeriod = "0";
        
        [Parameter("Daily turnover (min)")]
        private readonly string _turnover = "0";

        [Parameter("Daily turnover period")]
        private readonly string _turnoverPeriod = "0";

        [Parameter("Market capitalization (M min)")]
        private readonly string _marketCap = null;

        [Parameter("Revenue growth% (R12 min)")]
        private readonly string _revenueGrowth = null;

        [Parameter("Revenue inverse growth% (R12 min)")]
        private readonly string _revenueInverseGrowth = null;

        [Parameter("Revenue trend% (R12 min)")]
        private readonly string _revenueTrend = null;

        [Parameter("Net income (R12 min)")]
        private readonly string _netIncome = null;

        [Parameter("Net income (Q min)")]
        private readonly string _netIncomeQuarter = null;

        [Parameter("Net income growth% (R12 min)")]
        private readonly string _netIncomeGrowth = null;

        [Parameter("Net income inverse growth% (R12 min)")]
        private readonly string _netIncomeInverseGrowth = null;

        [Parameter("Net income trend% (R12 min)")]
        private readonly string _netIncomeTrend = null;

        [Parameter("Net margin% (R12 min)")]
        private readonly string _netMargin = null;

        [Parameter("Net margin trend% (R12 min)")]
        private readonly string _netMarginTrend = null;

        [Parameter("Free cash flow margin% (R12 min)")]
        private readonly string _freeCashFlowMargin = null;

        [Parameter("Free cash flow margin trend% (R12 min)")]
        private readonly string _freeCashFlowMarginTrend = null;

        [Parameter("Return on equity% (R12 min)")]
        private readonly string _returnOnEquity = null;

        [Parameter("PE ratio (R12 min)")]
        private readonly string _peRatio = null;

        [Parameter("EP ratio (R12 min)")]
        private readonly string _epRatio = null;

        [Parameter("PS ratio (R12 min)")]
        private readonly string _psRatio = null;

        [Parameter("SP ratio (R12 min)")]
        private readonly string _spRatio = null;

        public override void Initialize()
        {
            if (string.IsNullOrEmpty(_symbols)) throw new ArgumentNullException(nameof(_symbols));
            if (string.IsNullOrEmpty(_security)) throw new ArgumentNullException(nameof(_security));
            if (string.IsNullOrEmpty(_resolution)) throw new ArgumentNullException(nameof(_resolution));
            if (string.IsNullOrEmpty(_market)) throw new ArgumentNullException(nameof(_market));

            SecurityType securityType = (SecurityType)Enum.Parse(typeof(SecurityType), _security);
            Resolution resolution = (Resolution)Enum.Parse(typeof(Resolution), _resolution);
            decimal fee = decimal.Parse(_fee, CultureInfo.InvariantCulture);
            long turnover = long.Parse(_turnover, CultureInfo.InvariantCulture);
            int turnoverPeriod = int.Parse(_turnoverPeriod, CultureInfo.InvariantCulture);
            int slots = int.Parse(_slots, CultureInfo.InvariantCulture);
            bool reinvest = bool.Parse(_reinvest);
            float rebalance = float.Parse(_rebalance, CultureInfo.InvariantCulture);
            int smaPeriod = int.Parse(_smaPeriod, CultureInfo.InvariantCulture);
            List<Symbol> symbols = _symbols
                .Split(';')
                .Select(x => QuantConnect.Symbol.Create(x, securityType, _market))
                .ToList();

            EnableAutomaticIndicatorWarmUp = true;
            UniverseSettings.Resolution = resolution;
            MarketHoursDatabase.Entry entry = MarketHoursDatabase.GetEntry(_market, (string)null, securityType);
            SetTimeZone(entry.DataTimeZone);
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols));
            SetPortfolioConstruction(new SlotPortfolio(
                slots: slots,
                reinvest: reinvest,
                rebalance: rebalance,
                smaPeriod: smaPeriod));
            SetExecution(new LimitExecution(slots));
            SetRiskManagement(new NullRiskManagementModel());
            SetBenchmark(QuantConnect.Symbol.Create("OMXSPI.ST", securityType, _market));
            FeeModel feeModel = fee < 1 ? new PercentFeeModel(fee) : new ConstantFeeModel(fee);
            SetSecurityInitializer(security =>
            {
                security.FeeModel = feeModel;
                security.FillModel = new TouchFill();
            });
            SetWarmUp((int)(1.1 * turnoverPeriod), Resolution.Daily);
            SetAlpha(new MultiSignalAlpha(InsightDirection.Up, resolution, symbols,
                (symbol) => new TurnoverSignal(turnoverPeriod, turnover),
                (symbol) => new FundamentalSignal(
                    this,
                    symbol,
                    marketCap: _marketCap,
                    netIncome :_netIncome,
                    netIncomeQuarter: _netIncomeQuarter,
                    netIncomeGrowth: _netIncomeGrowth,
                    netIncomeInverseGrowth: _netIncomeInverseGrowth,
                    netIncomeTrend: _netIncomeTrend,
                    revenueGrowth : _revenueGrowth,
                    revenueInverseGrowth: _revenueInverseGrowth,
                    revenueTrend: _revenueTrend,
                    netMargin : _netMargin,
                    netMarginTrend : _netMarginTrend,
                    freeCashFlowMargin : _freeCashFlowMargin,
                    freeCashFlowMarginTrend: _freeCashFlowMarginTrend,
                    returnOnEquity: _returnOnEquity,
                    peRatio: _peRatio,
                    epRatio : _epRatio,
                    psRatio: _psRatio,
                    spRatio: _spRatio)));
        }

        public override void OnEndOfAlgorithm()
        {
            PortfolioConstruction.CreateTargets(this, null);
        }
    }
}
