/*
 * Copyright 2021 Capnode AB
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
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    public class PositionModel
    {
        public PositionModel()
        {
        }

        /// <summary>
        /// Symbol of the Asset
        /// </summary>
        [DataMember]
        public SymbolModel Symbol { get; set; }

        /// <summary>
        /// Number of shares.
        /// </summary>
        [DataMember]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Average entry price.
        /// </summary>
        [DataMember]
        public decimal AveragePrice { get; set; }

        /// <summary>
        /// Last updated price.
        /// </summary>
        [DataMember]
        public decimal MarketPrice { get; set; }

        /// <summary>
        /// Currency for the price
        /// </summary>
        [DataMember]
        public string PriceCurrency { get; set; }

        /// <summary>
        /// Market value of position.
        /// </summary>
        [DataMember]
        public decimal MarketValue { get; set; }

        /// <summary>
        /// Entry value of position.
        /// </summary>
        [DataMember]
        public decimal EntryValue { get; set; }

        /// <summary>
        /// Gets the utc time the price was opened.
        /// </summary>
        [DataMember]
        public DateTime EntryTime { get; set; }

        /// <summary>
        /// Gets the utc time the price was last updated.
        /// </summary>
        [DataMember]
        public DateTime UpdateTime { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is PositionModel other)) return false;
            if (!Symbol.Equals(other.Symbol)) return false;
            if (Quantity != other.Quantity) return false;
            if (AveragePrice != other.AveragePrice) return false;
            if (MarketPrice != other.MarketPrice) return false;
            if (PriceCurrency != other.PriceCurrency) return false;
            if (MarketValue != other.MarketValue) return false;
            if (EntryValue != other.EntryValue) return false;
            if (EntryTime != other.EntryTime) return false;
            if (UpdateTime != other.UpdateTime) return false;

            return true;
        }
    }
}
