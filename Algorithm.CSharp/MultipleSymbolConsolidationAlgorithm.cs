/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example structure for structuring an algorithm with indicator and consolidator data for many tickers.
    /// </summary>
    /// <meta name="tag" content="consolidating data" />
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="strategy example" />
    public class MultipleSymbolConsolidationAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// This is the period of bars we'll be creating
        /// </summary>
        private readonly TimeSpan _barPeriod = TimeSpan.FromMinutes(10);
        /// <summary>
        /// This is the period of our sma indicators
        /// </summary>
        private readonly int _simpleMovingAveragePeriod = 10;
        /// <summary>
        /// This is the number of consolidated bars we'll hold in symbol data for reference
        /// </summary>
        private readonly int _rollingWindowSize = 10;
        /// <summary>
        /// Holds all of our data keyed by each symbol
        /// </summary>
        private readonly Dictionary<string, SymbolData> _data = new Dictionary<string, SymbolData>();
        /// <summary>
        /// Contains all of our equity symbols
        /// </summary>
        private IReadOnlyList<string> _equitySymbols = new List<string>
        {
            "AAPL",
            "SPY",
            "IBM"
        };
        /// <summary>
        /// Contains all of our forex symbols
        /// </summary>
        private IReadOnlyList<string> _forexSymbols = new List<string>
        {
            "EURUSD",
            "USDJPY",
            "EURGBP",
            "EURCHF",
            "USDCAD",
            "USDCHF",
            "AUDUSD",
            "NZDUSD",
        };

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        /// <seealso cref="QCAlgorithm.SetStartDate(System.DateTime)"/>
        /// <seealso cref="QCAlgorithm.SetEndDate(System.DateTime)"/>
        /// <seealso cref="QCAlgorithm.SetCash(decimal)"/>
        public override void Initialize()
        {
            SetStartDate(2014, 12, 01);
            SetEndDate(2015, 02, 01);

            // initialize our equity data
            foreach (var symbol in _equitySymbols)
            {
                var equity = AddEquity(symbol);
                _data.Add(symbol, new SymbolData(equity.Symbol, _barPeriod, _rollingWindowSize));
            }

            // initialize our forex data
            foreach (var symbol in _forexSymbols)
            {
                var forex = AddForex(symbol);
                _data.Add(symbol, new SymbolData(forex.Symbol, _barPeriod, _rollingWindowSize));
            }

            // loop through all our symbols and request data subscriptions and initialize indicatora
            foreach (var kvp in _data)
            {
                // this is required since we're using closures below, for more information
                // see: http://stackoverflow.com/questions/14907987/access-to-foreach-variable-in-closure-warning
                var symbolData = kvp.Value;

                // define a consolidator to consolidate data for this symbol on the requested period
                var consolidator = symbolData.Symbol.SecurityType == SecurityType.Equity
                    ? (IDataConsolidator)new TradeBarConsolidator(_barPeriod)
                    : (IDataConsolidator)new QuoteBarConsolidator(_barPeriod);

                // define our indicator
                symbolData.SMA = new SimpleMovingAverage(CreateIndicatorName(symbolData.Symbol, "SMA" + _simpleMovingAveragePeriod, Resolution.Minute), _simpleMovingAveragePeriod);
                // wire up our consolidator to update the indicator
                consolidator.DataConsolidated += (sender, baseData) =>
                {
                    // 'bar' here is our newly consolidated data
                    var bar = (IBaseDataBar)baseData;
                    // update the indicator
                    symbolData.SMA.Update(bar.Time, bar.Close);
                    // we're also going to add this bar to our rolling window so we have access to it later
                    symbolData.Bars.Add(bar);
                };

                // we need to add this consolidator so it gets auto updates
                SubscriptionManager.AddConsolidator(symbolData.Symbol, consolidator);
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public override void OnData(Slice slice)
        {
            // loop through each symbol in our structure
            foreach (var symbolData in _data.Values)
            {
                // this check proves that this symbol was JUST updated prior to this OnData function being called
                if (symbolData.IsReady && symbolData.WasJustUpdated(Time))
                {
                    if (!Portfolio[symbolData.Symbol].Invested)
                    {
                        MarketOrder(symbolData.Symbol, 1);
                    }
                }
            }
        }

        /// <summary>
        /// End of a trading day event handler. This method is called at the end of the algorithm day (or multiple times if trading multiple assets).
        /// </summary>
        /// <remarks>Method is called 10 minutes before closing to allow user to close out position.</remarks>
        public override void OnEndOfDay(Symbol symbol)
        {
            int i = 0;
            foreach (var kvp in _data.OrderBy(x => x.Value.Symbol))
            {
                // we have too many symbols to plot them all, so plot ever other
                if (kvp.Value.IsReady && ++i%2 == 0)
                {
                    Plot(kvp.Value.Symbol.ToString(), kvp.Value.SMA);
                }
            }
        }

        /// <summary>
        /// Contains data pertaining to a symbol in our algorithm
        /// </summary>
        public class SymbolData
        {
            /// <summary>
            /// This symbol the other data in this class is associated with
            /// </summary>
            public Symbol Symbol { get; init; }
            /// <summary>
            /// A rolling window of data, data needs to be pumped into Bars by using Bars.Update( tradeBar ) and
            /// can be accessed like:
            ///  mySymbolData.Bars[0] - most first recent piece of data
            ///  mySymbolData.Bars[5] - the sixth most recent piece of data (zero based indexing)
            /// </summary>
            public RollingWindow<IBaseDataBar> Bars { get; init; }
            /// <summary>
            /// The period used when populating the Bars rolling window.
            /// </summary>
            public TimeSpan BarPeriod { get; init; }
            /// <summary>
            /// The simple moving average indicator for our symbol
            /// </summary>
            public SimpleMovingAverage SMA { get; set; }

            /// <summary>
            /// Initializes a new instance of SymbolData
            /// </summary>
            public SymbolData(Symbol symbol, TimeSpan barPeriod, int windowSize)
            {
                Symbol = symbol;
                BarPeriod = barPeriod;
                Bars = new RollingWindow<IBaseDataBar>(windowSize);
            }

            /// <summary>
            /// Returns true if all the data in this instance is ready (indicators, rolling windows, ect...)
            /// </summary>
            public bool IsReady
            {
                get { return Bars.IsReady && SMA.IsReady; }
            }

            /// <summary>
            /// Returns true if the most recent trade bar time matches the current time minus the bar's period; this
            /// indicates that Update() was just called on this instance.
            /// </summary>
            /// <param name="current">The current algorithm time</param>
            /// <returns>True if this instance was just updated with new data, false otherwise</returns>
            public bool WasJustUpdated(DateTime current)
            {
                return Bars.Count > 0 && Bars[0].Time == current - BarPeriod;
            }
        }
    }
}
