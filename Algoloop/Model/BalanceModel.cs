/*
 * Copyright 2021 Capnode AB
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
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class BalanceModel : ModelBase
    {

        [Category("Balance")]
        [DisplayName("Currency")]
        [Description("Currency of asset.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string Currency { get; set; }

        [Category("Balance")]
        [DisplayName("Cash")]
        [Description("Total unbound capital")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public decimal Cash { get; set; }

        [Category("Balance")]
        [DisplayName("Equity")]
        [Description("Total bound and unbound capital excluding loans.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public decimal Equity { get; set; }
    }
}
