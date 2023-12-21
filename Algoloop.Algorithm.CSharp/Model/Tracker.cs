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
    internal class Tracker : PortfolioConstructionModel
    {
        private readonly bool _logOrder = false;

        private readonly decimal _initialCash;
        private readonly int _slots;
        private readonly bool _reinvest;
        private readonly decimal _rebalance;
        private readonly List<MarketOrder> _orders = new();
        private readonly List<Trade> _trades = new();
        private readonly List<Trade> _holdings = new();
        private readonly List<PortfolioTarget> _targets = new();

        private decimal _cash = 0;
        private decimal _reserved = 0;

        public Tracker(decimal initialCash, int slots, bool reinvest, decimal rebalance)
        {
            _initialCash = initialCash;
            _cash = initialCash;
            _slots = slots;
            _reinvest = reinvest;
            _rebalance = rebalance;
        }

        public decimal Scale { get; set; } = 1;

        private decimal FreeCash => _cash - _reserved;

        // Create list of PortfolioTarget objects from Insights
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            if (insights == null)
            {
                LiquidateHoldings(algorithm);
                return null;
            }

            // Remove targets for symbols that have quantity zero
            _targets.RemoveAll(m => m.Quantity == 0);

            // Execute pending orders
            ProcessOrders(algorithm);

            IEnumerable<Insight> toplist = insights.Take(_slots);
//            LogInsights(algorithm, toplist);

            // Exit position if not in toplist
            CloseNotInList(algorithm, toplist);

            // Add new position if new in toplist
            AddNotInList(algorithm, toplist);

            // Rebalance portfolio
            if (Scale > 0)
            {
                RebalanceInList(algorithm, toplist);
            }

            //algorithm.Log($"{algorithm.Time:d} slots={_holdings.Count} cash={_cash:0.00} reserved={_reserved:0.00}");

            if (_holdings.Count > _slots) throw new ApplicationException($"{algorithm.Time:d} Too many positions");
            if (FreeCash.SmartRounding() < 0) throw new ApplicationException($"{algorithm.Time:d} Negative balance {FreeCash:0.00}");

            return _targets;
        }

        public decimal GetEquity(QCAlgorithm algorithm)
        {
            decimal equity = _cash;
            foreach (Trade trade in _holdings)
            {
                Security security = algorithm.Securities[trade.Symbol];
                decimal value = security.Price * trade.Quantity;
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
            foreach (Trade trade in _holdings)
            {
                var order = new MarketOrder(trade.Symbol, trade.Quantity, trade.ExitTime, trade.ExitPrice);
                Security security = algorithm.Securities[trade.Symbol];
                decimal fee = Fee(order, security);
                trade.ExitPrice = security.Close;
                trade.ExitTime = algorithm.UtcTime;
                decimal profit = trade.ExitPrice * trade.Quantity - fee;
                _cash += profit;
                _trades.Add(trade);
                if (_logOrder)
                {
                    algorithm.Log($"{algorithm.Time:d} Sell {trade.Symbol} {trade.Quantity} @ {trade.ExitPrice}");
                }

                AddTarget(trade.Symbol, 0);
            }

            _holdings.Clear();
        }

        private void ProcessOrders(QCAlgorithm algorithm)
        {
            var time = algorithm.Time;
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
                    {
                        continue;
                    }
                }

                // Max fee is stored in order Tag field
                if (!decimal.TryParse(order.Tag, out decimal fee)) continue;
                Trade trade = _holdings.FirstOrDefault(m => m.Symbol.Equals(order.Symbol));
                if (order.Direction == OrderDirection.Sell)
                {
                    if (trade == null) throw new NotImplementedException("Short trading is not supported");
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
                            if (!_holdings.Remove(trade)) throw new ApplicationException($"Can not remove {order.Symbol}");
                            if (_logOrder)
                            {
                                algorithm.Log($"{trade.ExitTime:d} Sell {trade.Symbol} {trade.Quantity} @ {price}");
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
                            algorithm.Log($"{sellTrade.ExitTime:d} Sell rebalance {sellTrade.Symbol} {sellTrade.Quantity} @ {price}");
                        }
                    }
                    else
                    {
                        RemoveTarget(order.Symbol); 
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

                            _holdings.Add(trade);
                            if (_logOrder)
                            {
                                algorithm.Log($"{trade.EntryTime:d} Buy {trade.Symbol} {trade.Quantity} @ {price}");
                            }
                        }
                        else // Rebalance up
                        {
                            value += trade.Quantity * trade.EntryPrice;
                            trade.Quantity += order.Quantity;
                            trade.EntryPrice = value / trade.Quantity;
                            algorithm.Log($"{algorithm.Time:d} Buy rebalance {order.Symbol} {order.Quantity} @ {price}");
                        }
                    }
                    else
                    {
                        RemoveTarget(order.Symbol);
                    }
                }
            }

            _orders.Clear();
            _reserved = 0;
        }

        private void CloseNotInList(QCAlgorithm algorithm, IEnumerable<Insight> toplist)
        {
            foreach (Trade trade in _holdings)
            {
                if (toplist.Any(m => m.Symbol.Equals(trade.Symbol))) continue;

                Security security = algorithm.Securities[trade.Symbol];
                var order = new MarketOrder(trade.Symbol, -trade.Quantity, algorithm.Time, security.Close);
                decimal fee = Fee(order, security);
                decimal cost = order.Value + fee;
                if (cost > 0)
                {
                    if (FreeCash - cost < 0) continue;
                    _reserved += cost;
                }

                order = new MarketOrder(trade.Symbol, -trade.Quantity, algorithm.Time, security.Close, fee.ToString());
                _orders.Add(order);

                AddTarget(trade.Symbol, 0);
            }

            foreach (PortfolioTarget target in _targets.ToList())
            {
                if (target.Quantity == 0) continue;
                if (toplist.Any(m => m.Symbol.Equals(target.Symbol))) continue;
                RemoveTarget(target.Symbol);
            }
        }

        private void AddNotInList(QCAlgorithm algorithm, IEnumerable<Insight> toplist)
        {
            int freeSlots = _slots - _holdings.Count;
            double? weights = toplist.Sum(m => m.Weight);
            if (weights == default || weights == 0) return;
            foreach (Insight insight in toplist)
            {
                if (freeSlots == 0) break;
                if (_holdings.Any(m => m.Symbol.Equals(insight.Symbol))) continue;
                decimal size = (_reinvest ? GetEquity(algorithm) : _initialCash) * (decimal)insight.Weight / (decimal)weights;
                size = Math.Min(size, FreeCash);
                if (size <= 0) continue;
                Security security = algorithm.Securities[insight.Symbol];
                decimal quantity = decimal.Floor(size / security.Close);
                var order = new MarketOrder(insight.Symbol, quantity, algorithm.Time, security.Close);
                decimal fee = Fee(order, security);
                quantity = decimal.Floor((size - fee) / security.Close);
                if (quantity <= 0) continue;
                order = new MarketOrder(insight.Symbol, quantity, algorithm.Time, security.Close, fee.ToString());
                if (order.Value < 2 * fee) continue;
                _reserved += order.Value + fee;
                freeSlots--;
                _orders.Add(order);

                if (Scale > 0)
                {
                    AddTarget(insight.Symbol, Scale * quantity);
                }
            }
        }

        private void RebalanceInList(QCAlgorithm algorithm, IEnumerable<Insight> toplist)
        {
            double? weights = toplist.Sum(m => m.Weight);
            if (weights == default || weights == 0) return;
            foreach (Insight insight in toplist)
            {
                Trade trade = _holdings.FirstOrDefault(m => m.Symbol.Equals(insight.Symbol));
                if (trade == null) continue;

                Security security = algorithm.Securities[insight.Symbol];
                decimal modelSize = (_reinvest ? GetEquity(algorithm) : _initialCash) * (decimal)insight.Weight / (decimal)weights;
                decimal modelQuantity = decimal.Floor(modelSize / security.Close);

                if (_rebalance > 0 && trade.Quantity <= ((1 - _rebalance) * modelQuantity).SmartRounding())
                {
                    // Rebalance up
                    decimal quantity = decimal.Floor(FreeCash / security.Close);
                    if (quantity < modelQuantity) continue; // Not enough cash to rebalance up
                    decimal diff = modelQuantity - trade.Quantity;
                    var order = new MarketOrder(insight.Symbol, diff, algorithm.Time, security.Close);
                    decimal fee = Fee(order, security);
                    decimal value = order.Value + fee;
                    if (_reserved + value > _cash) continue;
                    order = new MarketOrder(insight.Symbol, diff, algorithm.Time, security.Close, fee.ToString());
                    if (order.Value < 2 * fee) continue;
                    _reserved += value;
                    _orders.Add(order);

                    if (Scale > 0)
                    {
                        AddTarget(insight.Symbol, Scale * modelQuantity);
                    }

                }
                else if (_rebalance > 0 && trade.Quantity >= ((1 + _rebalance) * modelQuantity).SmartRounding())
                {
                    // Rebalance down
                    decimal diff = modelQuantity - trade.Quantity;
                    var order = new MarketOrder(insight.Symbol, diff, algorithm.Time, security.Close);
                    decimal fee = Fee(order, security);
                    decimal cost = order.Value + fee;
                    if (cost > 0)
                    {
                        if (FreeCash - cost < 0) continue;
                        _reserved += cost;
                    }

                    order = new MarketOrder(insight.Symbol, diff, algorithm.Time, security.Close, fee.ToString());
                    _orders.Add(order);

                    AddTarget(insight.Symbol, Scale * modelQuantity);
                }
            }
        }

        private static decimal Fee(MarketOrder order, Security security)
        {
            OrderFee orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, order));
            decimal fee = decimal.Ceiling(orderFee);
            return fee;
        }

        private void RemoveTarget(Symbol symbol)
        {
            _targets.RemoveAll(m => m.Symbol.Equals(symbol));
        }

        private void AddTarget(Symbol symbol, decimal quantity)
        {
            RemoveTarget(symbol);
            quantity = quantity > 0 ? decimal.Floor(quantity) : decimal.Ceiling(quantity);
            _targets.Add(new PortfolioTarget(symbol, quantity));
        }
    }
}
