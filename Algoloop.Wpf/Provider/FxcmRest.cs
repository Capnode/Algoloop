/*
 * Copyright 2021 Capnode AB
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

using Algoloop.Brokerages.FxcmRest;
using Algoloop.Model;
using QuantConnect.Logging;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Algoloop.Wpf.Provider
{
    public class FxcmRest : ProviderBase
    {
        private FxcmClient _api;

        public override IReadOnlyList<AccountModel> Login(ProviderModel broker, SettingModel settings)
        {
            Contract.Requires(broker != null);

            _api = new FxcmClient(broker.Access, broker.ApiKey);
            if (!_api.LoginAsync())
            {
                return (IReadOnlyList<AccountModel>)Enumerable.Empty<AccountModel>();
            }

            IReadOnlyList<AccountModel> accounts = _api.GetAccountsAsync().Result;
            return accounts;
        }

        public override void Logout()
        {
            if (!_api.LoginAsync())
            {
                Log.Error("{0}: Logout failed", GetType().Name);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _api?.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
