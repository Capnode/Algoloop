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

using Algoloop.Model;
using QuantConnect.Orders;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace Algoloop.Wpf.ViewModel
{
    public class OrderViewModel : ViewModel
    {
        public OrderViewModel(Order order)
        {
            Contract.Requires(order != null);
            Model = new OrderModel();
            Update(order);
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public OrderViewModel(OrderModel orderModel)
        {
            Model = orderModel;
        }

        public OrderViewModel(OrderEvent message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            Model = new OrderModel();
            Update(message);
        }

        public OrderModel Model { get; set; }

        public int Id
        {
            get => Model.Id;
            set
            {
                Model.Id = value;
                RaisePropertyChanged(() => Id);
            }
        }

        /// <summary>
        /// Order id to process before processing this order.
        /// </summary>
        public int ContingentId
        {
            get => Model.ContingentId;
            set
            {
                Model.ContingentId = value;
                RaisePropertyChanged(() => ContingentId);
            }
        }

        /// <summary>
        /// Brokerage Id for this order for when the brokerage splits orders into multiple pieces
        /// </summary>
        public Collection<string> BrokerId
        {
            get => Model.BrokerId;
            set
            {
                Model.BrokerId = value;
                RaisePropertyChanged(() => BrokerId);
            }
        }

        /// <summary>
        /// Symbol of the Asset
        /// </summary>
        public string Symbol
        {
            get => Model.Symbol;
            set
            {
                Model.Symbol = value;
                RaisePropertyChanged(() => Symbol);
            }
        }

        /// <summary>
        /// Price of the Order.
        /// </summary>
        public decimal Price
        {
            get => Model.Price;
            set
            {
                Model.Price = value;
                RaisePropertyChanged(() => Price);
            }
        }

        /// <summary>
        /// Limit price of the Order.
        /// </summary>
        public decimal? LimitPrice
        {
            get
            {
                if (Model.LimitPrice == 0)
                {
                    return null;
                }

                return Model.LimitPrice;
            }
            set
            {
                Model.LimitPrice = value ?? 0;
                RaisePropertyChanged(() => LimitPrice);
            }
        }

        /// <summary>
        /// Currency for the order price
        /// </summary>
        public string PriceCurrency
        {
            get => Model.PriceCurrency;
            set
            {
                Model.PriceCurrency = value;
                RaisePropertyChanged(() => PriceCurrency);
            }
        }

        /// <summary>
        /// Gets the utc time the order was created.
        /// </summary>
        public DateTime Time
        {
            get => Model.Time;
            set
            {
                Model.Time = value;
                RaisePropertyChanged(() => Time);
            }
        }

        /// <summary>
        /// Gets the utc time the last fill was received, or null if no fills have been received
        /// </summary>
        public DateTime? LastFillTime
        {
            get => Model.LastFillTime;
            set
            {
                Model.LastFillTime = value;
                RaisePropertyChanged(() => LastFillTime);
            }
        }

        /// <summary>
        /// Gets the utc time this order was last updated, or null if the order has not been updated.
        /// </summary>
        public DateTime? LastUpdateTime
        {
            get => Model.LastUpdateTime;
            set
            {
                Model.LastUpdateTime = value;
                RaisePropertyChanged(() => LastUpdateTime);
            }
        }

        /// <summary>
        /// Gets the utc time this order was canceled, or null if the order was not canceled.
        /// </summary>
        public DateTime? CanceledTime
        {
            get => Model.CanceledTime;
            set
            {
                Model.CanceledTime = value;
                RaisePropertyChanged(() => CanceledTime);
            }
        }

        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        public decimal Quantity
        {
            get => Model.Quantity;
            set
            {
                Model.Quantity = value;
                RaisePropertyChanged(() => Quantity);
            }
        }

        /// <summary>
        /// Order Type
        /// </summary>
        public OrderType Type
        {
            get => Model.Type;
            set
            {
                Model.Type = value;
                RaisePropertyChanged(() => Type);
            }
        }

        /// <summary>
        /// Status of the Order
        /// </summary>
        public OrderStatus Status
        {
            get => Model.Status;
            set
            {
                Model.Status = value;
                RaisePropertyChanged(() => Status);
            }
        }

        /// <summary>
        /// Order Time In Force
        /// </summary>
        public TimeInForce TimeInForce
        {
            get => Model.TimeInForce;
            set
            {
                Model.TimeInForce = value;
                RaisePropertyChanged(() => TimeInForce);
            }
        }

        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        public string Tag
        {
            get => Model.Tag;
            set
            {
                Model.Tag = value;
                RaisePropertyChanged(() => Tag);
            }
        }

        /// <summary>
        /// Additional properties of the order
        /// </summary>
        public OrderProperties Properties
        {
            get => Model.Properties;
            set
            {
                Model.Properties = value;
                RaisePropertyChanged(() => Properties);
            }
        }

        /// <summary>
        /// The symbol's security type
        /// </summary>
        public string SecurityType
        {
            get => Model.SecurityType;
            set
            {
                Model.SecurityType = value;
                RaisePropertyChanged(() => SecurityType);
            }
        }

        /// <summary>
        /// Order Direction Property based off Quantity.
        /// </summary>
        public string Direction
        {
            get => Model.Direction;
            set
            {
                Model.Direction = value;
                RaisePropertyChanged(() => Direction);
            }
        }

        /// <summary>
        /// Gets the executed value of this order. If the order has not yet filled,
        /// then this will return zero.
        /// </summary>
        public decimal OrderValue
        {
            get => Model.OrderValue;
            set
            {
                Model.OrderValue = value;
                RaisePropertyChanged(() => OrderValue);
            }
        }

        /// <summary>
        /// Gets the price data at the time the order was submitted
        /// </summary>
        public OrderSubmissionData OrderSubmissionData
        {
            get => Model.OrderSubmissionData;
            set
            {
                Model.OrderSubmissionData = value;
                RaisePropertyChanged(() => OrderSubmissionData);
            }
        }

        /// <summary>
        /// Returns true if the order is a marketable order.
        /// </summary>
        public bool IsMarketable
        {
            get => Model.IsMarketable;
            set
            {
                Model.IsMarketable = value;
                RaisePropertyChanged(() => IsMarketable);
            }
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
            Time = order.Time;
            LastFillTime = order.LastFillTime;
            LastUpdateTime = order.LastUpdateTime;
            CanceledTime = order.CanceledTime;
            Quantity = order.Quantity;
            Type = order.Type;
            Status = order.Status;
            TimeInForce = order.TimeInForce;
            Tag = order.Tag;
            Properties = (OrderProperties)order.Properties;
            SecurityType = order.SecurityType.ToString();
            Direction = order.Direction.ToString();
            OrderValue = order.Value;
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
            TimeInForce = order.TimeInForce;
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
    }
}
