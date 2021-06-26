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
using System;
using System.Diagnostics.Contracts;

namespace Algoloop.Wpf.Provider
{
    public class FxcmRest : ProviderBase
    {
        private FxcmClient _api;

        public override void Login(ProviderModel provider)
        {
            Log.Trace($"Login {provider.Provider}");
            base.Login(provider);
            Contract.Requires(provider != null);
            _api = new FxcmClient(provider.Access, provider.ApiKey);
            _api.Login();
        }

        public override void Logout()
        {
            _api.Logout();
        }

        public override void GetAccounts(ProviderModel provider, Action<object> update)
        {
             _api.GetAccountsAsync(update).Wait();
        }

        public override void GetMarketData(ProviderModel provider, Action<object> update)
        {
            _api.GetSymbolsAsync(update).Wait();
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                if (_api != null)
                {
                    _api.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
