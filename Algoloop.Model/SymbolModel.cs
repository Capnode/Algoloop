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

using Algoloop.Model.Converter;
using QuantConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class SymbolModel : ModelBase, IComparable
    {
        public SymbolModel()
        {
        }

        public SymbolModel(Symbol symbol)
        {
            Id = symbol.ID.Symbol;
            Name = symbol.ID.Symbol;
            Market = symbol.ID.Market;
            Security = symbol.SecurityType;
        }

        public SymbolModel(string id, string market, SecurityType security)
        {
            Id = id;
            Name = id;
            Market = market;
            Security = security;
        }

        public SymbolModel(SymbolModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            Active = model.Active;
            Id = model.Id;
            Name = model.Name;
            Market = model.Market;
            Security = model.Security;
        }

        /// <summary>
        /// Operator to handle database upgrade
        /// </summary>
        /// <param name="symbol"></param>
        public static implicit operator SymbolModel(string symbol)
        {
            return new SymbolModel(symbol, string.Empty, SecurityType.Base);
        }

        [DataMember]
        public string Id { get; set; } = string.Empty;

        [DataMember]
        public bool Active { get; set; } = true;

        [DataMember]
        public string Name { get; set; } = "name";

        [DataMember]
        public string Market { get; set; }

        [DataMember]
        public SecurityType Security { get; set; }

        [DataMember]
        public IDictionary<string, object> Properties { get; set; }

        /// <summary>
        /// Make sure property Id is valid
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = Name;
                Debug.Assert(!string.IsNullOrEmpty(Id));
            }
        }

        public void Refresh()
        {
        }

        public int CompareTo(object obj)
        {
            var a = obj as SymbolModel;
            int result = string.Compare(Id, a?.Id, StringComparison.OrdinalIgnoreCase);
            if (result != 0) return result;
            result = string.Compare(Name, a?.Name, StringComparison.OrdinalIgnoreCase);
            if (result != 0) return result;
            result = string.Compare(Market, a?.Market, StringComparison.OrdinalIgnoreCase);
            if (result != 0) return result;
            return Security.CompareTo(a?.Security);
        }

        public override string ToString()
        {
            return $"{Security} {Market} {Id} {Name}";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SymbolModel other)) return false;
            if (Id != other.Id) return false;
            if (Active != other.Active) return false;
            if (Name != other.Name) return false;
            if (Market != other.Market) return false;
            if (Security != other.Security) return false;
            if (!Collection.Equals(Properties, other.Properties)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            // credit: http://stackoverflow.com/a/263416/677735
            unchecked // Overflow is fine, just wrap
            {
                int hash = 41;
                // Suitable nullity checks etc, of course :)
                if (Id != null)
                    hash = hash * 59 + Id.GetHashCode();
                hash = hash * 59 + Active.GetHashCode();
                if (Name != null)
                    hash = hash * 59 + Name.GetHashCode();
                if (Market != null)
                    hash = hash * 59 + Market.GetHashCode();
                hash = hash * 59 + Security.GetHashCode();
                if (Properties != null)
                    hash = hash * 59 + Properties.GetHashCode();
                return hash;
            }
        }
    }
}
