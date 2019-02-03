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

using GalaSoft.MvvmLight;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;

namespace Algoloop.ViewModel
{
    public class OrderViewModel : ViewModelBase
    {
        private IReadOnlyList<string> _brokerId;
        private string _securityType;
        private string _symbolCurrency;
        private decimal _averagePrice;
        private decimal _quantity;
        private decimal _marketPrice;
        private decimal _conversionRate;
        private decimal _marketValue;
        private decimal _unrealizedPnL;
        private int _id;
        private int _contingentId;
        private decimal _price;
        private string _symbol;
        private string _priceCurrency;
        private DateTime _time;
        private DateTime? _lastFillTime;
        private DateTime? _lastUpdateTime;
        private DateTime? _canceledTime;
        private OrderType _orderType;
        private OrderStatus _status;
        private TimeInForce _timeInForce;
        private string _tag;
        private IOrderProperties _properties;
        private string _direction;
        private decimal _value;
        private OrderSubmissionData _orderSubmissionData;
        private bool _isMarketable;

        public OrderViewModel(Order order)
        {
            Update(order);
        }

        public OrderViewModel(OrderEvent message)
        {
            Update(message);
        }

        public int Id
        {
            get => _id;
            set => Set(ref _id, value);
        }

        /// <summary>
        /// Order id to process before processing this order.
        /// </summary>
        public int ContingentId
        {
            get => _contingentId;
            set => Set(ref _contingentId, value);
        }

        /// <summary>
        /// Brokerage Id for this order for when the brokerage splits orders into multiple pieces
        /// </summary>
        public IReadOnlyList<string> BrokerId
        {
            get => _brokerId;
            set => Set(ref _brokerId, value);
        }

        /// <summary>
        /// Symbol of the Asset
        /// </summary>
        public string Symbol
        {
            get => _symbol;
            set => Set(ref _symbol, value);
        }

        /// <summary>
        /// Price of the Order.
        /// </summary>
        public decimal Price
        {
            get => _price;
            set => Set(ref _price, value);
        }

        /// <summary>
        /// Currency for the order price
        /// </summary>
        public string PriceCurrency
        {
            get => _priceCurrency;
            set => Set(ref _priceCurrency, value);
        }

        /// <summary>
        /// Gets the utc time the order was created.
        /// </summary>
        public DateTime Time
        {
            get => _time;
            set => Set(ref _time, value);
        }

        /// <summary>
        /// Gets the utc time the last fill was received, or null if no fills have been received
        /// </summary>
        public DateTime? LastFillTime
        {
            get => _lastFillTime;
            set => Set(ref _lastFillTime, value);
        }

        /// <summary>
        /// Gets the utc time this order was last updated, or null if the order has not been updated.
        /// </summary>
        public DateTime? LastUpdateTime
        {
            get => _lastUpdateTime;
            set => Set(ref _lastUpdateTime, value);
        }

        /// <summary>
        /// Gets the utc time this order was canceled, or null if the order was not canceled.
        /// </summary>
        public DateTime? CanceledTime
        {
            get => _canceledTime;
            set => Set(ref _canceledTime, value);
        }

        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        public decimal Quantity
        {
            get => _quantity;
            set => Set(ref _quantity, value);
        }

        /// <summary>
        /// Order Type
        /// </summary>
        public OrderType Type
        {
            get => _orderType;
            set => Set(ref _orderType, value);
        }

        /// <summary>
        /// Status of the Order
        /// </summary>
        public OrderStatus Status
        {
            get => _status;
            set => Set(ref _status, value);
        }

        /// <summary>
        /// Order Time In Force
        /// </summary>
        public TimeInForce TimeInForce
        {
            get => _timeInForce;
            set => Set(ref _timeInForce, value);
        }

        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        public string Tag
        {
            get => _tag;
            set => Set(ref _tag, value);
        }

        /// <summary>
        /// Additional properties of the order
        /// </summary>
        public IOrderProperties Properties
        {
            get => _properties;
            set => Set(ref _properties, value);
        }

        /// <summary>
        /// The symbol's security type
        /// </summary>
        public string SecurityType
        {
            get => _securityType;
            set => Set(ref _securityType, value);
        }

        /// <summary>
        /// Order Direction Property based off Quantity.
        /// </summary>
        public string Direction
        {
            get => _direction;
            set => Set(ref _direction, value);
        }

        /// <summary>
        /// Gets the executed value of this order. If the order has not yet filled,
        /// then this will return zero.
        /// </summary>
        public decimal Value
        {
            get => _value;
            set => Set(ref _value, value);
        }

        /// <summary>
        /// Gets the price data at the time the order was submitted
        /// </summary>
        public OrderSubmissionData OrderSubmissionData
        {
            get => _orderSubmissionData;
            set => Set(ref _orderSubmissionData, value);
        }

        /// <summary>
        /// Returns true if the order is a marketable order.
        /// </summary>
        public bool IsMarketable
        {
            get => _isMarketable;
            set => Set(ref _isMarketable, value);
        }

        public void Update(Order order)
        {
            Symbol = order.Symbol.ID.Symbol;

            Id = order.Id;
            ContingentId = order.ContingentId;
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
            TimeInForce = order.TimeInForce;
            Tag = order.Tag;
            Properties = order.Properties;
            SecurityType = order.SecurityType.ToString();
            Direction = order.Direction.ToString();
            Value = order.Value;
            OrderSubmissionData = order.OrderSubmissionData;
            IsMarketable = order.IsMarketable;
        }

        internal void Update(OrderEvent message)
        {
            Symbol = message.Symbol;
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
    }
}
