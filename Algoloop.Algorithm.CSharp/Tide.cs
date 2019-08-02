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

using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders.Fees;
using QuantConnect.Parameters;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Capnode.Algorithm.CSharp
{
    public class Tide : QCAlgorithm
    {
        [Parameter("symbols")]
        protected string _symbols = "EURUSD";

        [Parameter("resolution")]
        protected string _resolution = "Hour";

        [Parameter("market")]
        protected string _market = Market.FXCM;

        [Parameter("startdate")]
        protected string _startdate = "01/01/2018 00:00:00";

        [Parameter("enddate")]
        protected string _enddate = "01/01/2019 00:00:00";

        [Parameter("cash")]
        protected string _cash = "100000";

        [Parameter("OpenHourLong")]
        private string _openHourLong = "1";

        [Parameter("CloseHourLong")]
        private string _closeHourLong = "4";

        [Parameter("OpenHourShort")]
        private string _openHourShort = "4";

        [Parameter("CloseHourShort")]
        private string _closeHourShort = "7";

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash
        /// and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetTimeZone(NodaTime.DateTimeZone.Utc);
            Log($"Timezone {TimeZone}");

            // Standard parameters
            SetStartDate(DateTime.Parse(_startdate, CultureInfo.InvariantCulture));
            SetEndDate(DateTime.Parse(_enddate, CultureInfo.InvariantCulture));
            SetCash(int.Parse(_cash));

            Resolution resolution = Resolution.Hour;
            Enum.TryParse(_resolution, out resolution);

            // Algorithm parameters
            var openTimeLong = TimeSpan.FromHours(double.Parse(_openHourLong));
            var closeTimeLong = TimeSpan.FromHours(double.Parse(_closeHourLong));
            var openTimeShort = TimeSpan.FromHours(double.Parse(_openHourShort));
            var closeTimeShort = TimeSpan.FromHours(double.Parse(_closeHourShort));

            // Set zero transaction fees
            SetSecurityInitializer(s => s.SetFeeModel(new FxcmFeeModel()));

            // Universe Selection
            IEnumerable<Symbol> symbols = _symbols
                .Split(';')
                .Select(x => QuantConnect.Symbol.Create(x, SecurityType.Forex, _market));

            // Add symbols again to get updates. (Should not be needed)
            symbols.ToList().ForEach(m => AddForex(m.Value, resolution, _market));

            UniverseSettings.Resolution = resolution;
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols));

            // Alpha Model
            SetAlpha(new TideAlphaModel(resolution, openTimeLong, closeTimeLong, openTimeShort, closeTimeShort));

            // Portfolio Construction
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Execution
            SetExecution(new ImmediateExecutionModel());

            // Risk Management
            SetRiskManagement(new NullRiskManagementModel());

            Log($"Tide {openTimeLong.Hours} {closeTimeLong.Hours} {openTimeShort.Hours} {closeTimeShort.Hours} {string.Join(";", symbols.Select(m => m))}");
        }
    }

    public class TideAlphaModel : AlphaModel
    {
        private Resolution _resolution;
        private TimeSpan _openTimeLong;
        private TimeSpan _closeTimeLong;
        private TimeSpan _openTimeShort;
        private TimeSpan _closeTimeShort;

        public TideAlphaModel(
            Resolution resolution,
            TimeSpan openTimeLong,
            TimeSpan closeTimeLong,
            TimeSpan openTimeShort,
            TimeSpan closeTimeShort)
        {
            _resolution = resolution;
            _openTimeLong = openTimeLong;
            _closeTimeLong = closeTimeLong;
            _openTimeShort = openTimeShort;
            _closeTimeShort = closeTimeShort;
        }

        public List<Symbol> Symbols { get; } = new List<Symbol>();

        public override IEnumerable<Insight> Update(
            QCAlgorithm algorithm,
            Slice data)
        {
            var insights = new List<Insight>();
            foreach (var kvp in algorithm.ActiveSecurities)
            {
                Symbol symbol = kvp.Key;
                if (!Symbols.Contains(symbol)
                    || !algorithm.IsMarketOpen(symbol))
                {
                    continue;
                }

                TimeSpan now = data.Time.TimeOfDay;
                bool longInHours = InHours(_openTimeLong, now, _closeTimeLong);
                bool shortInHours = InHours(_openTimeShort, now, _closeTimeShort);

                SecurityHolding holding = algorithm.Portfolio[symbol];
                if (!holding.Invested && longInHours)
                {
                    TimeSpan duration = (_closeTimeLong - now).Subtract(TimeSpan.FromSeconds(1));
                    if (duration < TimeSpan.Zero)
                    {
                        duration = duration.Add(TimeSpan.FromHours(24));
                    }

                    insights.Add(Insight.Price(symbol, duration, InsightDirection.Up));
                    algorithm.Log($"Insight Up");
                }
                else if (!holding.Invested && shortInHours)
                {
                    TimeSpan duration = (_closeTimeShort - now).Subtract(TimeSpan.FromSeconds(1));
                    if (duration < TimeSpan.Zero)
                    {
                        duration = duration.Add(TimeSpan.FromHours(24));
                    }

                    insights.Add(Insight.Price(symbol, duration, InsightDirection.Down));
                    algorithm.Log($"Insight Down");
                }
                else if (holding.IsLong && !longInHours || holding.IsShort && !shortInHours)
                {
                    insights.Add(Insight.Price(symbol, _resolution, 1, InsightDirection.Flat));
                    algorithm.Log($"Insight Flat");
                }
            }

            return insights;
        }

        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            foreach (Symbol symbol in changes.RemovedSecurities.Select(x => x.Symbol))
            {
                Symbol prospect = Symbols.Find(m => m.Equals(symbol));
                if (prospect != null)
                {
                    Symbols.Remove(prospect);
                }
            }

            // Initialize data for added securities
            IEnumerable<Symbol> symbols = changes.AddedSecurities.Select(x => x.Symbol);
            foreach (Symbol symbol in symbols)
            {
                Debug.Assert(!Symbols.Exists(m => m.Equals(symbol)));
                Symbols.Add(symbol);
            }
        }

        private bool InHours(TimeSpan open, TimeSpan now, TimeSpan close)
        {
            if (open == close)
            {
                return false;
            }

            if (open < close)
            {
                return open <= now && now < close;
            }
            else
            {
                return now < close || open <= now;
            }
        }
    }
}