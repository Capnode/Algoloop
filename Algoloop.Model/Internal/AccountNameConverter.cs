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

using Microsoft.Toolkit.Mvvm.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Algoloop.Model.Internal
{
    internal class AccountNameConverter : TypeConverter
    {
        private readonly MarketsModel _markets;

        public AccountNameConverter()
        {         
            _markets = Ioc.Default.GetService<MarketsModel>();
        }

        public override bool GetStandardValuesSupported(
            ITypeDescriptorContext context)
        {
            // true means show a combobox
            return true;
        }

        public override bool GetStandardValuesExclusive(
            ITypeDescriptorContext context)
        {
            // true will limit to list. false will show the list, but allow free-form entry
            return true;
        }

        public override StandardValuesCollection GetStandardValues(
            ITypeDescriptorContext context)
        {
            // Request list of accounts
            IReadOnlyList<AccountModel> accounts = _markets.GetAccounts();
            List<string> list = accounts
                .Select(m => m.DisplayName)
                .ToList();

            list.Sort();

            return new StandardValuesCollection(list);
        }
    }
}
