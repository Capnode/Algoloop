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

using QuantConnect;
using System;
using System.Collections.Generic;
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
        public string Id { get; set; } = "id";

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
            return $"{Security} {Market} {Name}";
        }
    }
}
