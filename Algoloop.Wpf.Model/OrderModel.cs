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

using QuantConnect.Orders;
using QuantConnect.Orders.TimeInForces;
using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Algoloop.Wpf.Model
{
    public class OrderModel
    {
        public OrderModel()
        {
        }

        public OrderModel(Order order)
        {
            Id = order.Id;
            ContingentId = order.ContingentId;
            Symbol = order.Symbol.Value;
            BrokerId = new Collection<string>(order.BrokerId);
            Symbol = order.Symbol.Value;
            Price = order.Price;
            LimitPrice = (order as LimitOrder)?.LimitPrice ?? 0;
            PriceCurrency = order.PriceCurrency;
            Time = order.Time.ToLocalTime();
            LastFillTime = order.LastFillTime?.ToLocalTime();
            LastUpdateTime = order.LastUpdateTime?.ToLocalTime();
            CanceledTime = order.CanceledTime?.ToLocalTime();
            Quantity = order.Quantity;
            Type = order.Type;
            Status = order.Status;
            ValidUntil = ToValidUntil(order).ToLocalTime();
            Tag = order.Tag;
            Properties = (OrderProperties)order.Properties;
            SecurityType = order.SecurityType.ToString();
            Direction = order.Direction.ToString();
            OrderValue = order.Value;
            OrderSubmissionData = order.OrderSubmissionData;
            IsMarketable = order.IsMarketable;
        }

        [DataMember]
        public int Id { get; set; }

        /// <summary>
        /// Order id to process before processing this order.
        /// </summary>
        [DataMember]
        public int ContingentId { get; set; }

        /// <summary>
        /// Brokerage Id for this order for when the brokerage splits orders into multiple pieces
        /// </summary>
        [DataMember]
        public Collection<string> BrokerId { get; set; }

        /// <summary>
        /// Symbol of the Asset
        /// </summary>
        [DataMember]
        public string Symbol { get; set; }

        /// <summary>
        /// Price of the Order.
        /// </summary>
        [DataMember]
        public decimal Price { get; set; }

        /// <summary>
        /// Limit price of the Order.
        /// </summary>
        [DataMember]
        public decimal LimitPrice { get; set; }

        /// <summary>
        /// Currency for the order price
        /// </summary>
        [DataMember]
        public string PriceCurrency { get; set; }

        /// <summary>
        /// Gets the utc time the order was created.
        /// </summary>
        [DataMember]
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets the utc time the last fill was received, or null if no fills have been received
        /// </summary>
        [DataMember]
        public DateTime? LastFillTime { get; set; }

        /// <summary>
        /// Gets the utc time this order was last updated, or null if the order has not been updated.
        /// </summary>
        [DataMember]
        public DateTime? LastUpdateTime { get; set; }

        /// <summary>
        /// Gets the utc time this order was canceled, or null if the order was not canceled.
        /// </summary>
        [DataMember]
        public DateTime? CanceledTime { get; set; }

        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        [DataMember]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Order Type
        /// </summary>
        [DataMember]
        public OrderType Type { get; set; }

        /// <summary>
        /// Status of the Order
        /// </summary>
        [DataMember]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Order valid until.
        /// </summary>
        [DataMember]
        public DateTime ValidUntil { get; set; }

        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        [DataMember]
        public string Tag { get; set; }

        /// <summary>
        /// Additional properties of the order
        /// </summary>
        [DataMember]
        public OrderProperties Properties { get; set; }

        /// <summary>
        /// The symbol's security type
        /// </summary>
        [DataMember]
        public string SecurityType { get; set; }

        /// <summary>
        /// Order Direction Property based off Quantity.
        /// </summary>
        [DataMember]
        public string Direction { get; set; }

        /// <summary>
        /// Gets the executed value of this order. If the order has not yet filled,
        /// then this will return zero.
        /// </summary>
        [DataMember]
        public decimal OrderValue { get; set; }

        /// <summary>
        /// Gets the price data at the time the order was submitted
        /// </summary>
        [DataMember]
        public OrderSubmissionData OrderSubmissionData { get; set; }

        /// <summary>
        /// Returns true if the order is a marketable order.
        /// </summary>
        [DataMember]
        public bool IsMarketable { get; set; }

        private static DateTime ToValidUntil(Order order)
        {
            if (order.TimeInForce is DayTimeInForce)
            {
                //TODO: Fix also for next day orders
                return order.Time.Date.AddDays(1).AddTicks(-1);
            }
            else if (order.TimeInForce is GoodTilCanceledTimeInForce)
            {
                return DateTime.MaxValue;
            }
            else if (order.TimeInForce is GoodTilDateTimeInForce date)
            {
                return date.Expiry;
            }
            return DateTime.MinValue;
        }
    }
}
