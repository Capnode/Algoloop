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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class AccountModel : ModelBase
    {
        private BrokerageType _brokerage;
        public enum AccountType { Backtest, Paper, Live };
        public enum BrokerageType { Fxcm };
        public enum AccessType { Demo, Real };

        [DisplayName("Account name")]
        [Description("Name of the account.")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string Name { get; set; } = "Account";

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public bool Active { get; set; }

        [Category("Account")]
        [DisplayName("Brokerage")]
        [Description("Name of the broker.")]
        [RefreshProperties(RefreshProperties.All)]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public BrokerageType Brokerage
        {
            get => _brokerage;
            set
            {
                _brokerage = value;
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
        [DisplayName("Account number")]
        [Description("Account number.")]
        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public string Id { get; set; } = string.Empty;

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public string DataFolder { get; set; }

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<OrderModel> Orders { get; } = new Collection<OrderModel>();

        public void Refresh()
        {
            switch (Brokerage)
            {
                case BrokerageType.Fxcm:
                    SetBrowsable("Access", true);
                    SetBrowsable("Login", true);
                    SetBrowsable("Password", true);
                    SetBrowsable("Id", true);
                    break;
                default:
                    SetBrowsable("Access", false);
                    SetBrowsable("Login", false);
                    SetBrowsable("Password", false);
                    SetBrowsable("Id", false);
                    break;
            }
        }
    }
}