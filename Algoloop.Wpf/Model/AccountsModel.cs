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
            new AccountModel() { Name = AccountModel.AccountType.Backtest.ToString() },
            new AccountModel() { Name = AccountModel.AccountType.Paper.ToString() }
        };

        [Description("Major Version - Increment at breaking change.")]
        [Browsable(false)]
        [DataMember]
        public int Version { get; set; }

        [Browsable(false)]
        [DataMember]
        public Collection<AccountModel> Accounts { get; } = new Collection<AccountModel>();

        internal void Copy(AccountsModel accountsModel)
        {
            Accounts.Clear();
            foreach (AccountModel account in accountsModel.Accounts)
            {
                Accounts.Add(account);
            }
        }

        internal AccountModel FindAccount(string account)
        {
            return Accounts.FirstOrDefault(m => m.Name.Equals(account, StringComparison.OrdinalIgnoreCase));
        }

        internal IReadOnlyList<AccountModel> GetAccounts()
        {
            IEnumerable<AccountModel> accounts = Accounts.Concat(_standardAccounts);
            return accounts.ToList();
        }
    }
}
