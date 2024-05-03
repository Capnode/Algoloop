/*
 * Copyright 2018 Capnode AB
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Algoloop.Wpf.Model
{
    [Serializable]
    [DataContract]
    public class AccountModel : ModelBase
    {
        public enum AccountType { Backtest, Paper, Live };

        public AccountModel()
        {
        }

        public AccountModel(string name)
        {
            Id = name;
            Name = name;
        }

        [Browsable(false)]
        [ReadOnly(false)]
        public ProviderModel Provider { get; set; }

        [Category("Account")]
        [DisplayName("Number")]
        [Description("Number or identity of the account.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string Id { get; set; } = string.Empty;

        [Category("Account")]
        [DisplayName("Name")]
        [Description("Name of the account.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string Name { get; set; } = string.Empty;

        [Category("Account")]
        [DisplayName("Active")]
        [Description("Account is tradable.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public bool Active { get; set; }

        [Browsable(false)]
        [ReadOnly(false)]
        public string DisplayName => Provider == default ? Name : $"{Provider.Name}/{Name}";

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<BalanceModel> Balances { get; } = new Collection<BalanceModel>();

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<PositionModel> Positions { get; } = new Collection<PositionModel>();

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<OrderModel> Orders { get; } = new Collection<OrderModel>();

        public override bool Equals(object obj)
        {
            if (obj is not AccountModel other) return false;
            if (Provider != other.Provider) return false;
            if (Id != other.Id) return false;
            if (Name != other.Name) return false;
            if (Active != other.Active) return false;
            if (!Collection.Equals(Balances, other.Balances)) return false;
            if (!Collection.Equals(Positions, other.Positions)) return false;
            if (!Collection.Equals(Orders, other.Orders)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            // credit: http://stackoverflow.com/a/263416/677735
            unchecked // Overflow is fine, just wrap
            {
                int hash = 41;
                // Suitable nullity checks etc, of course :)
                if (Provider != null)
                    hash = hash * 59 + Provider.GetHashCode();
                if (Id != null)
                    hash = hash * 59 + Id.GetHashCode();
                if (Name != null)
                    hash = hash * 59 + Name.GetHashCode();
                hash = hash * 59 + Active.GetHashCode();
                if (Balances != null)
                    hash = hash * 59 + Balances.GetHashCode();
                if (Positions != null)
                    hash = hash * 59 + Positions.GetHashCode();
                if (Orders != null)
                    hash = hash * 59 + Orders.GetHashCode();
                return hash;
            }
        }
    }
}
