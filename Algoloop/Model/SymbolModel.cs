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

        public SymbolModel(SymbolModel model)
        {
            Name = model.Name;
            Active = model.Active;
        }

        [DataMember]
        public bool Active { get; set; } = true;

        [DataMember]
        public string Name { get; set; } = "symbol";

        [DataMember]
        public IDictionary<string, object> Properties { get; set; }

        internal void Refresh()
        {
        }

        public int CompareTo(object obj)
        {
            var a = obj as SymbolModel;
            return string.Compare(Name, a?.Name);
        }
    }
}
