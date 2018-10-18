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
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class ParameterModel
    {
        public ParameterModel()
        {
        }

        public ParameterModel(ParameterModel model)
        {
            Enabled = model.Enabled;
            Name = model.Name;
            Value = model.Value;
            Multiple = model.Multiple;
            Range = model.Range;
        }

        [DataMember]
        public bool Enabled { get; set; } = true;

        [DataMember]
        public string Name { get; set; } = string.Empty;

        [DataMember]
        public string Value { get; set; } = string.Empty;

        [DataMember]
        public bool Multiple { get; set; }

        [DataMember]
        public string Range { get; set; } = string.Empty;
    }
}
