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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class BrokerModel : ModelBase
    {
        private const string _fxcm = "fxcm";
        private string _provider;

        public enum AccessType { Demo, Real };

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public bool Active { get; set; }

        [Category("Broker")]
        [DisplayName("Broker name")]
        [Description("Name of the broker login.")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string Name { get; set; } = "Broker";

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
                _provider = value?.ToLowerInvariant();
                Refresh();
            }
        }

        [Category("Broker")]
        [DisplayName("Type")]
        [Description("Type of account at the broker.")]
        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public AccessType Access { get; set; }

        [Category("Broker")]
        [DisplayName("Login")]
        [Description("User login.")]
        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public string Login { get; set; } = string.Empty;

        [Category("Broker")]
        [DisplayName("Password")]
        [Description("User login password.")]
        [PasswordPropertyText(true)]
        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public string Password { get; set; } = string.Empty;

        [Category("Broker")]
        [DisplayName("API key")]
        [Description("User API key.")]
        [PasswordPropertyText(true)]
        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public string ApiKey { get; set; } = string.Empty;

        [Category("Broker")]
        [DisplayName("Accounts")]
        [Description("List of accounts.")]
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
            Accounts.Clear();
            foreach (AccountModel account in accounts)
            {
                account.Broker = this;
                Accounts.Add(account);
            }
        }
    }
}