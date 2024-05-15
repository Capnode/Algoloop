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

namespace Algoloop.Wpf.Model
{
    [Serializable]
    [DataContract]
    public class ListModel : ModelBase, IComparable
    {
        public ListModel()
        {
        }

        public ListModel(string name)
        {
            Id = name;
            Name = name;
        }

        public ListModel(IEnumerable<SymbolModel> symbols)
        {
            foreach (SymbolModel symbol in symbols)
            {
                Symbols.Add(symbol);
            }
        }

        [DisplayName("List id")]
        [Description("Id of the list")]
        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public string Id { get; set; }

        [DisplayName("List name")]
        [Description("Name of the list")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string Name { get; set; } = "New list";

        [DisplayName("Auto")]
        [Description("Auto generated list")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public bool Auto { get; set; }

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<SymbolModel> Symbols { get; } = new Collection<SymbolModel>();

        public void Update(ListModel model)
        {
            if (Symbols.Equals(model.Symbols)) return;
            Symbols.Clear();
            foreach (SymbolModel symbol in model.Symbols)
            {
                Symbols.Add(symbol);
            }
        }

        public int CompareTo(object obj)
        {
            var a = obj as ListModel;
            return string.Compare(Name, a?.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (obj is not ListModel other) return false;
            return (Id ?? Name) == (other.Id ?? other.Name);
        }
    }
}
