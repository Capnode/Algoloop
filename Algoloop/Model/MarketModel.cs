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

using Algoloop.ViewSupport;
using QuantConnect;
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
        public enum MarketType { None, CryptoIQ, Dukascopy, Fxcm, FxcmVolume, Gdax, Google, IB, IEX, Kraken, Oanda, QuandBitfinex, Yahoo };
        public enum AccessType { No_login, Demo, Real };

        [Category("Data provider")]
        [DisplayName("Market name")]
        [Description("Name of the market.")]
        [DataMember]
        public string Name { get; set; } = "Market";

        [Category("Data provider")]
        [DisplayName("Provider")]
        [Description("Name of the data provider.")]
        [DataMember]
        public MarketType Provider { get; set; }

        [Category("Account")]
        [DisplayName("Access type")]
        [Description("Type of login account at data provider.")]
        [DataMember]
        public AccessType Access { get; set; }

        [Category("Account")]
        [DisplayName("Login")]
        [Description("User login.")]
        [DataMember]
        public string Login { get; set; } = string.Empty;

        [Category("Account")]
        [DisplayName("Password")]
        [Description("User login password.")]
        [PasswordPropertyText(true)]
        [DataMember]
        public string Password { get; set; } = string.Empty;

        [Category("Time")]
        [DisplayName("From date")]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        [DataMember]
        public DateTime FromDate { get; set; } = DateTime.Today;

        [Category("Time")]
        [DisplayName("Resolution")]
        [DataMember]
        public Resolution Resolution { get; set; }

        [Browsable(false)]
        [DataMember]
        public bool Active { get; set; }

        [Browsable(false)]
        [DataMember]
        public string DataFolder { get; set; }

        [Browsable(false)]
        [DataMember]
        public List<SymbolModel> Symbols { get; } = new List<SymbolModel>();
    }
}
