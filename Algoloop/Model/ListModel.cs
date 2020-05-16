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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class ListModel : ModelBase, IComparable
    {
        public ListModel()
        {
        }

        public ListModel(IEnumerable<SymbolModel> symbols)
        {
            foreach (SymbolModel symbol in symbols)
            {
                Symbols.Add(symbol);
            }
        }

        [DisplayName("List name")]
        [Description("Name of the list")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string Name { get; set; } = "New list";

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<SymbolModel> Symbols { get; } = new Collection<SymbolModel>();

        public int CompareTo(object obj)
        {
            var a = obj as ListModel;
            return string.Compare(Name, a?.Name, StringComparison.OrdinalIgnoreCase);
        }

        internal void Refresh()
        {
        }
    }
}
