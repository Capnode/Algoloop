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
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class MarketModel
    {
        public enum MarketType { Demo, Real };

        [DisplayName("Market name")]
        [Description("Name of the market.")]
        [DataMember]
        public string Name { get; set; } = "Market";

        [Browsable(false)]
        [DataMember]
        public bool Enabled { get; set; }

        [DataMember]
        [DisplayName("Market type")]
        [Description("Market type.")]
        public MarketType Type { get; set; }

        [DisplayName("Login")]
        [Description("User login.")]
        [DataMember]
        public string Login { get; set; } = string.Empty;

        [DisplayName("Password")]
        [Description("User login password.")]
        [PasswordPropertyText(true)]
        [DataMember]
        public string Password { get; set; } = string.Empty;

        [Browsable(false)]
        [DataMember]
        public List<SymbolModel> Symbols { get; } = new List<SymbolModel>();

        [Browsable(false)]
        [DataMember]
        public bool Completed { get; set; }
    }
}
