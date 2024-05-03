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

namespace Algoloop.Wpf.Model
{
    public enum Operator { None, Versus, Multiply, Divide, Plus, Minus };

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
            Operation = Operator.Versus;
        }

        [DataMember]
        public string Market { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public Operator Operation { get; set; }
    }
}
