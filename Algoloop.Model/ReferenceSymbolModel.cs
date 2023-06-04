/*
 * Copyright 2023 Capnode AB
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
    [Serializable]
    [DataContract]
    public class ReferenceSymbolModel : ModelBase
    {
        public ReferenceSymbolModel()
        {
        }

        public ReferenceSymbolModel(string market, string name)
        {
            Market = market;
            Name = name;
        }

        /// <summary>
        /// Operator to handle database upgrade
        /// </summary>
        /// <param name="symbol"></param>
        public static implicit operator ReferenceSymbolModel(string symbol)
        {
            var list = symbol.Split(":");
            if (list.Length < 2) return null;
            return new ReferenceSymbolModel(list[0], list[1]);
        }

        [DataMember]
        public string Market { get; set; }

        [DataMember]
        public string Name { get; set; }

    }
}
