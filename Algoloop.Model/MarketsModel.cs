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
    public class MarketsModel
    {
        public const int ActualVersion = 1;

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
        public Collection<ProviderModel> Markets { get; } = new Collection<ProviderModel>();

        public void Copy(MarketsModel marketsModel)
        {
            Markets.Clear();
            foreach (ProviderModel market in marketsModel.Markets)
            {
                Markets.Add(market);

                // Make sure all symbols has an id
                foreach (SymbolModel symbol in market.Symbols)
                {
                    symbol.Validate();
                }
            }
        }

        public AccountModel FindAccount(string name)
        {
            foreach (ProviderModel provider in Markets)
            {
                Collection<AccountModel> accounts = provider.Accounts;
                AccountModel account = accounts.FirstOrDefault(m => m.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (account != null) return account;
            }

            return null;
        }

        internal IReadOnlyList<AccountModel> GetAccounts()
        {
            var list = new List<AccountModel>();
            list.AddRange(_standardAccounts);
            foreach (ProviderModel provider in Markets)
            {
                Collection<AccountModel> accounts = provider.Accounts;
                list.AddRange(accounts);
            }

            return list;
        }

        public IReadOnlyList<ProviderModel> GetMarkets()
        {
            return Markets;
        }

        internal ProviderModel GetMarket(string market)
        {
            return Markets.FirstOrDefault(m => m.Name.Equals(market, StringComparison.OrdinalIgnoreCase));
        }
    }
}
