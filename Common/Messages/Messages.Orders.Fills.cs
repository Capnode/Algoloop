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
using System.Runtime.CompilerServices;

using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;

using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Orders.Fills"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.Fills.FillModel"/> class and its consumers or related classes
        /// </summary>
        public static class FillModel
        {
            /// <summary>
            /// Returns a string message warning saying the order was filled at stale price
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledAtStalePrice(Securities.Security security, Prices prices)
            {
                return Invariant($"Warning: fill at stale price ({prices.EndTime.ToStringInvariant()} {security.Exchange.TimeZone})");
            }

            /// <summary>
            /// Returns a string message saying the market never closes for the given symbol, and that an order of the given
            /// type cannot be submitted
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MarketNeverCloses(Securities.Security security, OrderType orderType)
            {
                return Invariant($"Market never closes for this symbol {security.Symbol}, can no submit a {nameof(orderType)} order.");
            }

            /// <summary>
            /// Returns a string message containing the given subscribedTypes
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static string SubscribedTypesToString(HashSet<Type> subscribedTypes)
            {
                return subscribedTypes == null
                    ? string.Empty
                    : Invariant($" SubscribedTypes: [{string.Join(",", subscribedTypes.Select(type => type.Name))}]");
            }

            /// <summary>
            /// Returns a string message saying it was impossible to get ask price to perform the fill for the given security symbol because
            /// no market data was found
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoMarketDataToGetAskPriceForFilling(Securities.Security security, HashSet<Type> subscribedTypes = null)
            {
                return Invariant($"Cannot get ask price to perform fill for {security.Symbol} because no market data was found.") +
                    SubscribedTypesToString(subscribedTypes);
            }

            /// <summary>
            /// Returns a string message saying it was impossible to get bid price to perform the fill for the given security symbol because
            /// no market data was found
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoMarketDataToGetBidPriceForFilling(Securities.Security security, HashSet<Type> subscribedTypes = null)
            {
                return Invariant($"Cannot get bid price to perform fill for {security.Symbol} because no market data was found.") +
                    SubscribedTypesToString(subscribedTypes);
            }

            /// <summary>
            /// Returns a string message saying it was impossible to perform a fill for the given security symbol because
            /// no data subscription was found
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoDataSubscriptionFoundForFilling(Securities.Security security)
            {
                return Invariant($"Cannot perform fill for {security.Symbol} because no data subscription were found.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.Fills.EquityFillModel"/> class and its consumers or related classes
        /// </summary>
        public static class EquityFillModel
        {
            /// <summary>
            /// String message saying: No trade with the OfficialOpen or OpeningPrints flag within the 1-minute timeout
            /// </summary>
            public static string MarketOnOpenFillNoOfficialOpenOrOpeningPrintsWithinOneMinute =
                "No trade with the OfficialOpen or OpeningPrints flag within the 1-minute timeout.";

            /// <summary>
            /// String message saying: No trade with the OfficialClose or ClosingPrints flag within the 1-minute timeout
            /// </summary>
            public static string MarketOnCloseFillNoOfficialCloseOrClosingPrintsWithinOneMinute =
                "No trade with the OfficialClose or ClosingPrints flag within the 1-minute timeout.";

            /// <summary>
            /// String message saying: No trade with the OfficialClose or ClosingPrints flag for data that does not include
            /// extended market hours
            /// </summary>
            public static string MarketOnCloseFillNoOfficialCloseOrClosingPrintsWithoutExtendedMarketHours =
                "No trade with the OfficialClose or ClosingPrints flag for data that does not include extended market hours.";

            /// <summary>
            /// Returns a string message saying the last data (of the given tick type) has been used to fill
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithLastTickTypeData(Tick tick)
            {
                return Invariant($"Fill with last {tick.TickType} data.");
            }

            /// <summary>
            /// Returns a string message warnning the user that no trade information was available, so the order was filled
            /// using quote data
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithQuoteData(Securities.Security security)
            {
                return Invariant($@"Warning: No trade information available at {security.LocalTime.ToStringInvariant()} {
                    security.Exchange.TimeZone}, order filled using Quote data");
            }

            /// <summary>
            /// Returns a string message warning the user that the fill is at stale price and that the order will
            /// be filled using quote tick data
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithQuoteTickData(Securities.Security security, Tick quoteTick)
            {
                return Invariant($@"Warning: fill at stale price ({quoteTick.EndTime.ToStringInvariant()} {
                    security.Exchange.TimeZone}), using Quote Tick data.");
            }

            /// <summary>
            /// Returns a string message warning the user that no quote information was available, so the order
            /// was filled using trade tick data
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithTradeTickData(Securities.Security security, Tick tradeTick)
            {
                return Invariant($@"Warning: No quote information available at {tradeTick.EndTime.ToStringInvariant()} {
                    security.Exchange.TimeZone}, order filled using Trade Tick data");
            }

            /// <summary>
            /// Returns a string message warning the user that the fill was at stale price, so quote bar data
            /// was used to fill the order
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithQuoteBarData(Securities.Security security, QuoteBar quoteBar)
            {
                return Invariant($@"Warning: fill at stale price ({quoteBar.EndTime.ToStringInvariant()} {
                    security.Exchange.TimeZone}), using QuoteBar data.");
            }

            /// <summary>
            /// Returns a string message warning the user that no quote information was available, so that trade bar
            /// data was used to fill the order
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithTradeBarData(Securities.Security security, TradeBar tradeBar)
            {
                return Invariant($@"Warning: No quote information available at {tradeBar.EndTime.ToStringInvariant()} {
                    security.Exchange.TimeZone}, order filled using TradeBar data");
            }

            /// <summary>
            /// Returns a string message saying that the order was filled using the open price due to a favorable gap
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithOpenDueToFavorableGap(Securities.Security security, TradeBar tradeBar)
            {
                return Invariant($@"Due to a favorable gap at {tradeBar.EndTime.ToStringInvariant()} {security.Exchange.TimeZone}, order filled using the open price ({tradeBar.Open})");
            }

            /// <summary>
            /// Returns a string message saying that the order was filled using the open price due to an unfavorable gap
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithOpenDueToUnfavorableGap(Securities.Security security, TradeBar tradeBar)
            {
                return Invariant($@"Due to an unfavorable gap at {tradeBar.EndTime.ToStringInvariant()} {security.Exchange.TimeZone}, order filled using the open price ({tradeBar.Open})");
            }
        }
    }
}
