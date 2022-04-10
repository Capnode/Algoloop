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
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Algoloop.ViewModel.Internal.Provider
{
    internal class FxcmRest : ProviderBase
    {
        private FxcmClient _api;
        private bool _symbolsUpdated;

        public override void Login(ProviderModel provider)
        {
            //Log.Trace($">{GetType().Name}:Login {provider.Provider}");
            base.Login(provider);
            Contract.Requires(provider != null);
            _api = new FxcmClient(provider.Access, provider.ApiKey);
            _api.Login();
            _symbolsUpdated = false;
            //Log.Trace($"<{GetType().Name}:Login {provider.Provider}");
        }

        public override void Logout()
        {
            _api.Logout().Wait();
        }

        public override void GetAccounts(ProviderModel provider, Action<object> update)
        {
            //Log.Trace($">{GetType().Name}:GetAccounts");
            _api.GetAccountsAsync(update).Wait();
            //Log.Trace($"<{GetType().Name}:GetAccounts");
        }

        public override void GetMarketData(ProviderModel provider, Action<object> update)
        {
            //Log.Trace($">{GetType().Name}:GetMarketData");
            DateTime now = DateTime.UtcNow;
            if (!_symbolsUpdated)
            {
                IReadOnlyList<SymbolModel> symbols = _api.GetSymbolsAsync().Result;
                var sym = _api.GetMarketDataAsync(update);
                UpdateSymbols(provider, symbols, true);
                update(provider.Symbols);
                _symbolsUpdated = true;
                _api.SubscribeMarketDataAsync(provider.Symbols, update).Wait();
            }

            provider.LastDate = now.ToLocalTime();
            //Log.Trace($"<{GetType().Name}:GetMarketData");
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
