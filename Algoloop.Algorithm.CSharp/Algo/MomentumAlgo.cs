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

namespace Algoloop.Algorithm.CSharp
{
    public class MomentumAlgo : QCAlgorithm
    {
        private const decimal Fee = 0.0025m;

        [Parameter("symbols")]
        private readonly string _symbols = "ABB.ST;ERIC-B.ST;ATCO-A.ST;SEB.ST";

        [Parameter("security")]
        private readonly string _security = "Equity";

        [Parameter("resolution")]
        private readonly string _resolution = "Daily";

        [Parameter("market")]
        private readonly string _market = Market.Borsdata;

        [Parameter("Period")]
        private readonly string _period = "0";

        [Parameter("Hold")]
        private readonly string _hold = "1";

        [Parameter("Slots")]
        private readonly string _slots = "1";

        [Parameter("Daily turnover (min)")]
        private readonly string _turnover = "0";

        [Parameter("Daily turnover period")]
        private readonly string _turnoverPeriod = "0";

        [Parameter("Reinvest")]
        private readonly string _reinvest = "false";

        [Parameter("Rebalance trigger (min)")]
        private readonly string _rebalance = "0";

        public override void Initialize()
        {
            SecurityType securityType = (SecurityType)Enum.Parse(typeof(SecurityType), _security);
            Resolution resolution = (Resolution)Enum.Parse(typeof(Resolution), _resolution);
            int period = int.Parse(_period, CultureInfo.InvariantCulture);
            int hold = int.Parse(_hold, CultureInfo.InvariantCulture);
            int slots = int.Parse(_slots, CultureInfo.InvariantCulture);
            long turnover = long.Parse(_turnover, CultureInfo.InvariantCulture);
            int turnoverPeriod = int.Parse(_turnoverPeriod, CultureInfo.InvariantCulture);
            bool reinvest = bool.Parse(_reinvest);
            float rebalance = float.Parse(_rebalance, CultureInfo.InvariantCulture);

            List<Symbol> symbols = _symbols
                .Split(';')
                .Select(x => QuantConnect.Symbol.Create(x, securityType, _market))
                .ToList();

            EnableAutomaticIndicatorWarmUp = true;
            SetTimeZone(NodaTime.DateTimeZone.Utc);
            UniverseSettings.Resolution = resolution;
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols));
            SetPortfolioConstruction(new SlotPortfolio(slots, reinvest, rebalance));
            SetExecution(new LimitExecution(slots));
            SetRiskManagement(new NullRiskManagementModel());
            SetBenchmark(QuantConnect.Symbol.Create("OMXSPI.ST", securityType, _market));
            SetSecurityInitializer(security =>
            {
                security.FeeModel = new PercentFeeModel(Fee);
                security.FillModel = new TouchFill();
            });
            SetAlpha(new MultiSignalAlpha(InsightDirection.Up, resolution, Math.Max(period, turnoverPeriod), hold, symbols,
                (symbol) => new TurnoverSignal(this, turnoverPeriod, turnover),
                (symbol) => new MomentumSignal(period)));
        }

        public override void OnEndOfAlgorithm()
        {
            Log(PortfolioConstruction.ToString());
        }
    }
}
