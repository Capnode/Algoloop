/*
 * Copyright 2018-2019 Capnode AB
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

using Algoloop.Wpf.Model;
using QuantConnect.Orders;
using QuantConnect.Orders.TimeInForces;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace Algoloop.Wpf.ViewModels
{
    public class OrderViewModel : ViewModelBase
    {
        private int _id;
        private int _contingentId;
        private Collection<string> _brokerId;
        private string _symbol;
        private decimal _price;
        private decimal? _limitPrice;
        private string _priceCurrency;
        private DateTime _time;
        private DateTime? _lastFillTime;
        private DateTime? _lastUpdateTime;
        private DateTime? _canceledTime;
        private decimal _quantity;
        private OrderType _type;
        private OrderStatus _status;
        private DateTime _validUntil;
        private string _tag;
        private OrderProperties _properties;
        private string _securityType;
        private string _direction;
        private decimal _orderValue;
        private OrderSubmissionData _orderSubmissionData;
        private bool _isMarketable;

        public OrderViewModel(Order order)
        {
            Contract.Requires(order != null);
            Update(order);
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public OrderViewModel(OrderModel order)
        {
            Update(order);
        }

        public OrderViewModel(OrderEvent message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            Update(message);
        }

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Order id to process before processing this order.
        /// </summary>
        public int ContingentId
        {
            get => _contingentId;
            set => SetProperty(ref _contingentId, value);
        }

        /// <summary>
        /// Brokerage Id for this order for when the brokerage splits orders into multiple pieces
        /// </summary>
        public Collection<string> BrokerId
        {
            get => _brokerId;
            set => SetProperty(ref _brokerId, value);
        }

        /// <summary>
        /// Symbol of the Asset
        /// </summary>
        public string Symbol
        {
            get => _symbol;
            set => SetProperty(ref _symbol, value);
        }

        /// <summary>
        /// Price of the Order.
        /// </summary>
        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        /// <summary>
        /// Limit price of the Order.
        /// </summary>
        public decimal? LimitPrice
        {
            get => _limitPrice;
            set => SetProperty(ref _limitPrice, value);
        }

        /// <summary>
        /// Currency for the order price
        /// </summary>
        public string PriceCurrency
        {
            get => _priceCurrency;
            set => SetProperty(ref _priceCurrency, value);
        }

        /// <summary>
        /// Gets the local time the order was created.
        /// </summary>
        public DateTime Time
        {
            get => _time;
            set => SetProperty(ref _time, value);
        }

        /// <summary>
        /// Gets the local time the last fill was received, or null if no fills have been received
        /// </summary>
        public DateTime? LastFillTime
        {
            get => _lastFillTime;
            set => SetProperty(ref _lastFillTime,  value);
        }

        /// <summary>
        /// Gets the local time this order was last updated, or null if the order has not been updated.
        /// </summary>
        public DateTime? LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }

        /// <summary>
        /// Gets the local time this order was canceled, or null if the order was not canceled.
        /// </summary>
        public DateTime? CanceledTime
        {
            get => _canceledTime;
            set => SetProperty(ref _canceledTime, value);
        }

        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        /// <summary>
        /// Order Type
        /// </summary>
        public OrderType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// Status of the Order
        /// </summary>
        public OrderStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// Order valid until local time
        /// </summary>
        public DateTime ValidUntil
        {
            get => _validUntil;
            set => SetProperty(ref _validUntil, value);
        }

        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        public string Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }

        /// <summary>
        /// Additional properties of the order
        /// </summary>
        public OrderProperties Properties
        {
            get => _properties;
            set => SetProperty(ref _properties, value);
        }

        /// <summary>
        /// The symbol's security type
        /// </summary>
        public string SecurityType
        {
            get => _securityType;
            set => SetProperty(ref _securityType, value);
        }

        /// <summary>
        /// Order Direction Property based off Quantity.
        /// </summary>
        public string Direction
        {
            get => _direction;
            set => SetProperty(ref _direction, value);
        }

        /// <summary>
        /// Gets the executed value of this order. If the order has not yet filled,
        /// then this will return zero.
        /// </summary>
        public decimal OrderValue
        {
            get => _orderValue;
            set => SetProperty(ref _orderValue, value);
        }

        /// <summary>
        /// Gets the price data at the time the order was submitted
        /// </summary>
        public OrderSubmissionData OrderSubmissionData
        {
            get => _orderSubmissionData;
            set => SetProperty(ref _orderSubmissionData, value);
        }

        /// <summary>
        /// Returns true if the order is a marketable order.
        /// </summary>
        public bool IsMarketable
        {
            get => _isMarketable;
            set => SetProperty(ref _isMarketable, value);
        }

        internal void Update(Order order)
        {
            Id = order.Id;
            ContingentId = order.ContingentId;
            Symbol = order.Symbol.Value;
            BrokerId = new Collection<string>(order.BrokerId);
            Symbol = order.Symbol.Value;
            Price = order.Price;
            LimitPrice = (order as LimitOrder)?.LimitPrice;
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
            OrderValue = order.Quantity * order.Price;
            OrderSubmissionData = order.OrderSubmissionData;
            IsMarketable = order.IsMarketable;
        }

        internal void Update(OrderModel order)
        {
            Id = order.Id;
            ContingentId = order.ContingentId;
            Symbol = order.Symbol;
            BrokerId = order.BrokerId;
            Symbol = order.Symbol;
            Price = order.Price;
            PriceCurrency = order.PriceCurrency;
            Time = order.Time;
            LastFillTime = order.LastFillTime;
            LastUpdateTime = order.LastUpdateTime;
            CanceledTime = order.CanceledTime;
            Quantity = order.Quantity;
            Type = order.Type;
            Status = order.Status;
            ValidUntil = order.ValidUntil;
            Tag = order.Tag;
            Properties = order.Properties;
            SecurityType = order.SecurityType.ToString(CultureInfo.InvariantCulture);
            Direction = order.Direction.ToString(CultureInfo.InvariantCulture);
            OrderValue = order.OrderValue;
            OrderSubmissionData = order.OrderSubmissionData;
            IsMarketable = order.IsMarketable;
        }

        internal void Update(OrderEvent message)
        {
            Symbol = message.Symbol.Value;
            Id = message.OrderId;
            Price = message.FillPrice;
            PriceCurrency = message.FillPriceCurrency;
            LastFillTime = message.UtcTime;
            LastUpdateTime = message.UtcTime;
            CanceledTime = message.UtcTime;
            Quantity = message.FillQuantity;
            Status = message.Status;
            Direction = message.Direction.ToString();
        }

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
