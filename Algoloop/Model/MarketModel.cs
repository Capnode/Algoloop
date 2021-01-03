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

using Algoloop.Support;
using QuantConnect;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class MarketModel : ModelBase
    {
        [NonSerialized]
        public Action ModelChanged;

        private string _provider;
        private const string _fxcm = "fxcm";

        public enum AccessType { Demo, Real };

        [Category("Data provider")]
        [DisplayName("Market name")]
        [Description("Name of the market.")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string Name { get; set; } = "Market";

        [Category("Data provider")]
        [DisplayName("Provider")]
        [Description("Name of the data provider.")]
        [TypeConverter(typeof(ProviderNameConverter))]
        [RefreshProperties(RefreshProperties.All)]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string Provider
        {
            get => _provider;
            set
            {
                Contract.Requires(value != null);
                _provider = value.ToLowerInvariant();
                Refresh();
            }
        }

        [Category("Account")]
        [DisplayName("Access type")]
        [Description("Type of login account at data provider.")]
        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public AccessType Access { get; set; }

        [Category("Account")]
        [DisplayName("Login")]
        [Description("User login.")]
        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public string Login { get; set; } = string.Empty;

        [Category("Account")]
        [DisplayName("Password")]
        [Description("User login password.")]
        [PasswordPropertyText(true)]
        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public string Password { get; set; } = string.Empty;

        [Category("Account")]
        [DisplayName("API key")]
        [Description("User API key.")]
        [PasswordPropertyText(true)]
        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public string ApiKey { get; set; } = string.Empty;

        [Category("Time")]
        [DisplayName("Last date")]
        [Description("Symbols are updated up to this date.")]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public DateTime LastDate { get; set; } = DateTime.Today;

        [Category("Time")]
        [DisplayName("Timeframe")]
        [Description("Download data timeframe period. Behaviour is dependent on actual data provider.")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public Resolution Resolution { get; set; }

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public bool Active { get; set; }

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<SymbolModel> Symbols { get; } = new Collection<SymbolModel>();

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<ListModel> Lists { get; } = new Collection<ListModel>();

        public void Refresh()
        {
            if (string.IsNullOrEmpty(Provider)) return;

            if (Provider.Equals(_fxcm, StringComparison.OrdinalIgnoreCase))
            {
                SetBrowsable("Access", true);
                SetBrowsable("Login", true);
                SetBrowsable("Password", true);
                SetBrowsable("ApiKey", false);
            }
            else
            {
                SetBrowsable("Access", false);
                SetBrowsable("Login", false);
                SetBrowsable("Password", false);
                SetBrowsable("ApiKey", false);
            }
        }

        internal void AddList(ListModel list)
        {
            Lists.Add(list);
            ModelChanged?.Invoke();
        }

        internal static IEnumerable<SymbolModel> GetActiveSymbols(ListModel list)
        {
            return list.Symbols.Where(m => m.Active);
        }
    }
}
