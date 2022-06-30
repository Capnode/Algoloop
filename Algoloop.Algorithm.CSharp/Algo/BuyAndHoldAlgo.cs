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
    public class BuyAndHoldAlgo : QCAlgorithm
    {
        private const decimal Fee = 0.0025m;

        [Parameter("symbols")]
        private readonly string _symbols = "ABB.ST;ERIC-B.ST;ATCO-A.ST;SEB.ST";

        [Parameter("security")]
        private readonly string _security = "Equity";

        [Parameter("resolution")]
        private readonly string _resolution = "Daily";

        [Parameter("market")]
        private readonly string _market = "Borsdata";

        [Parameter("Slots")]
        private readonly string _slots = "1";

        public override void Initialize()
        {
            SecurityType securityType = (SecurityType)Enum.Parse(typeof(SecurityType), _security);
            Resolution resolution = (Resolution)Enum.Parse(typeof(Resolution), _resolution);
            int slots = int.Parse(_slots, CultureInfo.InvariantCulture);

            List<Symbol> symbols = _symbols
                .Split(';')
                .Select(x => QuantConnect.Symbol.Create(x, securityType, _market))
                .ToList();

            EnableAutomaticIndicatorWarmUp = true;
            SetTimeZone(NodaTime.DateTimeZone.Utc);
            UniverseSettings.Resolution = resolution;
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols));
            SetPortfolioConstruction(new SlotPortfolio(slots, false, 0));
            SetExecution(new LimitExecution(slots));
            SetRiskManagement(new NullRiskManagementModel());
            SetBenchmark(QuantConnect.Symbol.Create("OMXSPI.ST", securityType, _market));
            SetSecurityInitializer(security =>
            {
                security.FeeModel = new PercentFeeModel(Fee);
                security.FillModel = new TouchFill();
            });
            SetAlpha(new MultiSignalAlpha(InsightDirection.Up, resolution, 0, 1, symbols));
        }

        public override void OnEndOfAlgorithm()
        {
            Log(PortfolioConstruction.ToString());
        }
    }
}
