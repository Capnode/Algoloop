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

using Algoloop.Model.Converter;
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
    public class ProviderModel : ModelBase
    {
        [NonSerialized]
        public Action ModelChanged;

        private string _provider;
        private const string _borsdata = "borsdata";
        private const string _avanza = "avanza";
        private const string _fxcm = "fxcm";
        private const string _fxcmrest = "fxcmrest";

        public enum AccessType { Demo, Real };

        [Category("Market provider")]
        [DisplayName("Name")]
        [Description("Name of the market provider.")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string Name { get; set; } = "Market";

        [Category("Market provider")]
        [DisplayName("Provider")]
        [Description("Type name of the market provider.")]
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
        [Description("Account are updated up to this time.")]
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
        public string DefaultAccountId { get; set; }

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<SymbolModel> Symbols { get; } = new Collection<SymbolModel>();

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<ListModel> Lists { get; } = new Collection<ListModel>();

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<AccountModel> Accounts { get; set; } = new Collection<AccountModel>();

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
            else if (Provider.Equals(_fxcmrest, StringComparison.OrdinalIgnoreCase))
            {
                SetBrowsable("Access", true);
                SetBrowsable("Login", false);
                SetBrowsable("Password", false);
                SetBrowsable("ApiKey", true);
            }
            else if (Provider.Equals(_borsdata, StringComparison.OrdinalIgnoreCase))
            {
                SetBrowsable("Access", false);
                SetBrowsable("Login", false);
                SetBrowsable("Password", false);
                SetBrowsable("ApiKey", true);
            }
            else if (Provider.Equals(_avanza, StringComparison.OrdinalIgnoreCase))
            {
                SetBrowsable("Access", false);
                SetBrowsable("Login", true);
                SetBrowsable("Password", true);
                SetBrowsable("ApiKey", true);
            }
            else
            {
                SetBrowsable("Access", false);
                SetBrowsable("Login", false);
                SetBrowsable("Password", false);
                SetBrowsable("ApiKey", false);
            }
        }

        public void UpdateAccounts(IEnumerable<AccountModel> accounts)
        {
            if (accounts == null) return;
            Accounts.Clear();
            foreach (AccountModel account in accounts)
            {
                account.Provider = this;
                Accounts.Add(account);
            }
        }

        public void UpdateSymbols(IEnumerable<SymbolModel> symbols)
        {
            if (symbols == null) return;
            Symbols.Clear();
            foreach (SymbolModel symbol in symbols)
            {
                Symbols.Add(symbol);
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
