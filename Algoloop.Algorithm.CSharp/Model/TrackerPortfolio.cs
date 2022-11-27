/*
 * Copyright 2022 Capnode AB
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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Statistics;

namespace Algoloop.Algorithm.CSharp.Model
{
    internal class TrackerPortfolio : PortfolioConstructionModel
    {
        private readonly decimal _initialCash;
        private readonly bool _logOrder = false;
        private readonly int _slots;
        private readonly bool _reinvest;
        private readonly decimal _rebalance;
        private readonly List<MarketOrder> _orders = new();
        private readonly List<Trade> _trades = new();
        private readonly Dictionary<Symbol, Trade> _holdings = new();

        private decimal _cash = 0;
        private decimal _reserved = 0;

        public TrackerPortfolio(decimal initialCash, int slots, bool reinvest, decimal rebalance)
        {
            _initialCash = initialCash;
            _cash = initialCash;
            _slots = slots;
            _reinvest = reinvest;
            _rebalance = rebalance;
        }

        private decimal FreeCash => _cash - _reserved;

        // Create list of PortfolioTarget objects from Insights
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        
        {
            if (insights == null)
            {
                LiquidateHoldings(algorithm);
                return null;
            }

            // Execute pending orders
            ProcessOrders(algorithm);

            IEnumerable<Insight> toplist = insights.Take(_slots);
//            LogInsights(algorithm, toplist);

            // Exit position if not in toplist
            CloseNotInList(algorithm, toplist);

            // Add new position if new in toplist
            AddNotInList(algorithm, toplist);

            // Rebalance portfolio
            RebalanceInList(algorithm, toplist);

            //algorithm.Log($"{algorithm.Time:d} slots={_holdings.Count} cash={_cash:0.00} reserved={_reserved:0.00}");

            if (_holdings.Count > _slots)
                throw new ApplicationException($"{algorithm.Time:d} Too many positions");
            if (FreeCash < -0.0001m)
                throw new ApplicationException($"{algorithm.Time:d} Negative balance {FreeCash:0.00}");
            return null;
        }

        public decimal GetEquity(QCAlgorithm algorithm)
        {
            decimal equity = _cash;
            foreach (KeyValuePair<Symbol, Trade> holding in _holdings)
            {
                Security security = algorithm.Securities[holding.Key];
                decimal value = security.Price * holding.Value.Quantity;
                equity += value;
            }

            return equity.SmartRounding();
        }

        private void LiquidateHoldings(QCAlgorithm algorithm)
        {
            if (_logOrder)
            {
                algorithm.Log("Liquidate holdings:");
            }

            // Liquidate holdings
            foreach (KeyValuePair<Symbol, Trade> holding in _holdings)
            {
                Security security = algorithm.Securities[holding.Key];
                Trade trade = holding.Value;
                trade.ExitPrice = security.Close;
                trade.ExitTime = algorithm.UtcTime;
                decimal profit = trade.ExitPrice * trade.Quantity;
                _cash += profit;
                _trades.Add(trade);
                if (_logOrder)
                {
                    algorithm.Log($"{algorithm.Time:d} Sell {holding.Key} {trade.Quantity} @ {trade.ExitPrice} cash={_cash:0.00}");
                }
            }
            _holdings.Clear();

            decimal cash = _initialCash;
            foreach (Trade trade in _trades)
            {
                decimal profit = trade.Quantity * (trade.ExitPrice - trade.EntryPrice);
                cash += profit;
                if (_logOrder)
                {
                    algorithm.Log($"Trade {trade.EntryTime:d} {trade.ExitTime:d} {trade.Symbol} Size={trade.Quantity} Entry={trade.EntryPrice} Exit={trade.ExitPrice} Profit={profit:0.00}");
                }
            }

            if (_logOrder)
            {
                algorithm.Log($"Summary _cash={_cash:0.00} cash={cash:0.00} diff={cash - _cash:0.00}");
            }
        }

        private void ProcessOrders(QCAlgorithm algorithm)
        {
            foreach (MarketOrder order in _orders)
            {
                // Trade today ?
                Security security = algorithm.Securities[order.Symbol];
                if (!security.Exchange.DateTimeIsOpen(security.LocalTime))
                {
                    // if we're not open at the current time exactly, check the bar size, this handle large sized bars (hours/days)
                    var currentBar = security.GetLastData();
                    if (security.LocalTime.Date != currentBar.EndTime.Date
                        || !security.Exchange.IsOpenDuringBar(currentBar.Time, currentBar.EndTime, false))
                        continue;
                }

                // Order fee
                decimal fee = Fee(order, security);

                _holdings.TryGetValue(order.Symbol, out Trade trade);
                if (order.Direction == OrderDirection.Sell)
                {
                    if (trade == null)
                        throw new NotImplementedException("Short trading is not supported");

                    decimal price = Math.Max(order.Price, security.Low);
                    decimal value = price * order.Quantity;
                    if (order.Price <= security.High)
                    {
                        _cash = _cash - value - fee;
                        trade.ExitTime = algorithm.Time;
                        trade.ExitPrice = price;
                        decimal diff = order.Quantity + trade.Quantity;
                        if (diff.Equals(0))
                        {
                            _trades.Add(trade);
                            if (!_holdings.Remove(trade.Symbol)) throw new ApplicationException($"Can not remove {order.Symbol}");
                            if (_logOrder)
                            {
                                algorithm.Log($"{trade.ExitTime:d} Sell {trade.Symbol} {trade.Quantity} @ {price} cash={_cash:0.00}");
                            }
                        }
                        else // Rebalance down
                        {
                            trade.Quantity += order.Quantity;
                            var sellTrade = new Trade
                            {
                                Symbol = order.Symbol,
                                EntryTime = trade.EntryTime,
                                EntryPrice = trade.EntryPrice,
                                Quantity = -order.Quantity,
                                ExitTime = algorithm.Time,
                                ExitPrice = price,
                            };
                            _trades.Add(sellTrade);
                            if (_logOrder)
                            {
                                algorithm.Log($"{sellTrade.ExitTime:d} Sell rebalance {sellTrade.Symbol} {sellTrade.Quantity} @ {price} cash={_cash:0.00}");
                            }
                        }
                    }
                }
                else if (order.Direction == OrderDirection.Buy)
                {
                    decimal price = Math.Min(order.Price, security.High);
                    decimal value = price * order.Quantity;
                    if (order.Price >= security.Low)
                    {
                        _cash = _cash - value - fee;
                        if (trade == null)
                        {
                            trade = new Trade
                            {
                                Symbol = order.Symbol,
                                EntryTime = algorithm.Time,
                                EntryPrice = price,
                                Quantity = order.Quantity,
                            };

                            _holdings.Add(order.Symbol, trade);
                            if (_logOrder)
                            {
                                algorithm.Log($"{trade.EntryTime:d} Buy {trade.Symbol} {trade.Quantity} @ {price} cash={_cash:0.00}");
                            }
                        }
                        else // Rebalance up
                        {
                            value += trade.Quantity * trade.EntryPrice;
                            trade.Quantity += order.Quantity;
                            trade.EntryPrice = value / trade.Quantity;
                            if (_logOrder)
                            {
                                algorithm.Log($"{algorithm.Time:d} Buy rebalance {order.Symbol} {order.Quantity} @ {price} cash={_cash:0.00}");
                            }
                        }
                    }
                }
            }

            _orders.Clear();
            _reserved = 0;
        }

        private void CloseNotInList(QCAlgorithm algorithm, IEnumerable<Insight> toplist)
        {
            foreach (KeyValuePair<Symbol, Trade> holding in _holdings.ToArray())
            {
                if (toplist.Any(m => m.Symbol.Equals(holding.Key)))
                    continue;

                Security security = algorithm.Securities[holding.Key];
                var order = new MarketOrder(holding.Key, -holding.Value.Quantity, algorithm.Time, security.Close);
                _orders.Add(order);
            }
        }

        private void AddNotInList(QCAlgorithm algorithm, IEnumerable<Insight> toplist)
        {
            int freeSlots = _slots - _holdings.Count;
            foreach (Insight insight in toplist)
            {
                if (freeSlots == 0)
                    break;

                if (_holdings.ContainsKey(insight.Symbol))
                    continue;

                decimal size = (_reinvest ? GetEquity(algorithm) / freeSlots : _initialCash) / _slots;
                size = Math.Min(size, FreeCash);
                if (size <= 0)
                    continue;

                Security security = algorithm.Securities[insight.Symbol];
                decimal quantity = decimal.Floor(size / security.Close);
                var order = new MarketOrder(insight.Symbol, quantity, algorithm.Time, security.Close);
                decimal fee = Fee(order, security);
                quantity = decimal.Floor((size - fee) / security.Close);
                if (quantity <= 0)
                    continue;

                order = new MarketOrder(insight.Symbol, quantity, algorithm.Time, security.Close);
                _reserved += order.Value + fee;
                freeSlots--;
                _orders.Add(order);
            }
        }

        private void RebalanceInList(QCAlgorithm algorithm, IEnumerable<Insight> toplist)
        {
            foreach (Insight insight in toplist)
            {
                if (!_holdings.TryGetValue(insight.Symbol, out Trade trade))
                    continue;

                Security security = algorithm.Securities[insight.Symbol];
                decimal modelSize = (_reinvest ? GetEquity(algorithm) : _initialCash) / _slots;
                decimal modelQuantity = decimal.Floor(modelSize / security.Close);

                if (_rebalance > 0 && trade.Quantity <= decimal.Round((1 - _rebalance) * modelQuantity, 6))
                {
                    // Rebalance up
                    decimal quantity = decimal.Floor(FreeCash / security.Close);
                    if (quantity < modelQuantity)
                        continue; // Not enough cash to rebalance up

                    decimal diff = modelQuantity - trade.Quantity;
                    var order = new MarketOrder(insight.Symbol, diff, algorithm.Time, security.Close);
                    decimal fee = Fee(order, security);
                    decimal value = order.Value + fee;
                    if (_reserved + value > _cash)
                        continue;

                    _reserved += value;
                    _orders.Add(order);
                }
                else if (_rebalance > 0 && trade.Quantity >= decimal.Round((1 + _rebalance) * modelQuantity, 6))
                {
                    // Rebalance down
                    decimal diff = modelQuantity - trade.Quantity;
                    var order = new MarketOrder(insight.Symbol, diff, algorithm.Time, security.Close);
                    _orders.Add(order);
                }
            }
        }

        private static decimal Fee(MarketOrder order, Security security)
        {
            OrderFee orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, order));
            decimal fee = decimal.Ceiling(orderFee);
            return fee;
        }
    }
}
