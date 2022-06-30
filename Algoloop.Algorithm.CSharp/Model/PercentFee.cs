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

using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an order fee model that returns a percentage order fee.
    /// </summary>
    public class PercentFeeModel : FeeModel
    {
        private readonly decimal _fee;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantFeeModel"/> class with the specified <paramref name="fee"/>
        /// </summary>
        /// <param name="fee">The fraction order fee used by the model</param>
        /// <param name="currency">The currency of the order fee</param>
        public PercentFeeModel(decimal fee)
        {
            _fee = Math.Abs(fee);
        }

        /// <summary>
        /// Get the fee for this order in quote currency
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in quote currency</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            Order order = parameters.Order;
            Security security = parameters.Security;

            // get order value in quote currency, then apply maker/taker fee factor
            decimal unitPrice = order.Direction == OrderDirection.Buy ? security.AskPrice : security.BidPrice;
            unitPrice *= security.SymbolProperties.ContractMultiplier;
            decimal fee = unitPrice * order.AbsoluteQuantity * _fee;
            return new OrderFee(new CashAmount(fee, security.QuoteCurrency.Symbol));
        }
    }
}
