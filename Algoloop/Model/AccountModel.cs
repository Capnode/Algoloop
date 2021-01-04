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
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class AccountModel : ModelBase
    {
        private string _provider;
        private const string _fxcm = "fxcm";

        public enum AccountType { Backtest, Paper, Live };
        public enum AccessType { Demo, Real };

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public bool Active { get; set; }

        [Category("Broker")]
        [DisplayName("Account name")]
        [Description("Name of the account.")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string Name { get; set; } = "Account";

        [Category("Broker")]
        [DisplayName("Provider")]
        [Description("Name of the broker.")]
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
        [DisplayName("Type")]
        [Description("Type of account at the broker.")]
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

        [Category("Account")]
        [DisplayName("Account number")]
        [Description("Account number.")]
        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public string Id { get; set; } = string.Empty;

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<OrderModel> Orders { get; } = new Collection<OrderModel>();

        public void Refresh()
        {
            if (string.IsNullOrEmpty(Provider)) return;

            if (Provider.Equals(_fxcm, StringComparison.OrdinalIgnoreCase))
            {
                SetBrowsable("Access", true);
                SetBrowsable("Login", true);
                SetBrowsable("Password", true);
                SetBrowsable("ApiKey", false);
                SetBrowsable("Id", true);
            }
            else
            {
                SetBrowsable("Access", false);
                SetBrowsable("Login", false);
                SetBrowsable("Password", false);
                SetBrowsable("ApiKey", false);
                SetBrowsable("Id", false);
            }
        }
    }
}