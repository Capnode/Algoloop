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

using QuantConnect.Securities;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class BalanceModel : ModelBase
    {
        public BalanceModel()
        {
        }

        public BalanceModel(CashAmount cashAmount)
        {
            Currency = cashAmount.Currency;
            Cash = cashAmount.Amount;
        }

        [Category("Balance")]
        [DisplayName("Currency")]
        [Description("Currency of asset.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string Currency { get; set; }

        [Category("Balance")]
        [DisplayName("Cash")]
        [Description("Total unbound capital")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public decimal Cash { get; set; }

        [Category("Balance")]
        [DisplayName("Equity")]
        [Description("Total bound and unbound capital excluding loans.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public decimal Equity { get; set; }

        [Category("Balance")]
        [DisplayName("Profit")]
        [Description("Total profit or loss on account since start.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public decimal Profit { get; set; }

        [Category("Balance")]
        [DisplayName("DayProfit")]
        [Description("Profit or loss on account today.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public decimal DayProfit { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is not BalanceModel other) return false;
            if (Currency != other.Currency) return false;
            if (Cash != other.Cash) return false;
            if (Equity != other.Equity) return false;
            if (Profit != other.Profit) return false;
            if (DayProfit != other.DayProfit) return false;

            return true;
        }

        public override int GetHashCode()
        {
            // credit: http://stackoverflow.com/a/263416/677735
            unchecked // Overflow is fine, just wrap
            {
                int hash = 41;
                // Suitable nullity checks etc, of course :)
                if (Currency != null)
                    hash = hash * 59 + Currency.GetHashCode();
                hash = hash * 59 + Cash.GetHashCode();
                hash = hash * 59 + Equity.GetHashCode();
                hash = hash * 59 + Profit.GetHashCode();
                hash = hash * 59 + DayProfit.GetHashCode();
                return hash;
            }
        }

    }
}
