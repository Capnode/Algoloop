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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [DataContract]
    public class AccountsModel
    {
        public const int version = 0;

        private static readonly AccountModel[] _standardAccounts = new[]
        {
            new AccountModel { Name = nameof(AccountModel.AccountType.Backtest) },
            new AccountModel { Name = nameof(AccountModel.AccountType.Paper) }
        };

        [Description("Major Version - Increment at breaking change.")]
        [Browsable(false)]
        [DataMember]
        public int Version { get; set; }

        [Browsable(false)]
        [DataMember]
        public Collection<ProviderModel> Brokers { get; } = new Collection<ProviderModel>();

        public void Copy(AccountsModel accountsModel)
        {
            Brokers.Clear();
            foreach (ProviderModel broker in accountsModel.Brokers)
            {
                Brokers.Add(broker);
            }
        }

        public AccountModel FindAccount(string name)
        {
            foreach (ProviderModel broker in Brokers)
            {
                Collection<AccountModel> accounts = broker.Accounts;
                AccountModel account = accounts.FirstOrDefault(m => m.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (account != null) return account;
            }

            return null;
        }

        internal IReadOnlyList<AccountModel> GetAccounts()
        {
            var list = new List<AccountModel>();
            list.AddRange(_standardAccounts);
            foreach (ProviderModel broker in Brokers)
            {
                Collection<AccountModel> accounts = broker.Accounts;
                list.AddRange(accounts);
            }

            return list;
        }
    }
}
