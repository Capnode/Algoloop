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

using Algoloop.Brokerages.Fxcm;
using Algoloop.Model;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Algoloop.ViewModel.Internal.Provider
{
    internal class FxcmRest : ProviderBase
    {
        private FxcmClient _api;
        private int? _symbolsHash = null;

        public override void Login(ProviderModel provider)
        {
            //Log.Trace($">{GetType().Name}:Login {provider.Provider}");
            base.Login(provider);
            Contract.Requires(provider != null);
            _api = new FxcmClient(provider.Access, provider.ApiKey);
            _api.Login();
            //Log.Trace($"<{GetType().Name}:Login {provider.Provider}");
        }

        public override void Logout()
        {
            _api.Logout().Wait();
        }

        public override void GetUpdate(ProviderModel provider, Action<object> update)
        {
            //Log.Trace($">{GetType().Name}:GetUpdate");
            DateTime now = DateTime.UtcNow;
            int hash = GetHashCode(provider.Symbols);
            if (hash != _symbolsHash)
            {
                Debug.WriteLine($"provider Active={provider.Symbols.Where(m => m.Active).Count()}");
                IReadOnlyList<SymbolModel> symbols = _api.GetSymbolsAsync().Result;
                Debug.WriteLine($"GetSymbols Active={symbols.Where(m => m.Active).Count()}");
                UpdateSymbols(provider, symbols, true);
                _api.SubscribeSymbolsAsync(provider.Symbols).Wait();
                _api.GetAccountsAsync(update).Wait();
                if (update != default)
                {
                    update(provider.Symbols);
                }

                _symbolsHash = hash;
            }

            //            provider.LastDate = now.ToLocalTime();
            //Log.Trace($"<{GetType().Name}:GetUpdate");
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

        private int GetHashCode(Collection<SymbolModel> symbols)
        {
            int hash = 41;
            foreach (SymbolModel symbol in symbols)
            {
                // Suitable nullity checks etc, of course :)
                if (symbol.Id != null)
                    hash = hash * 59 + symbol.Id.GetHashCode();
                hash = hash * 59 + symbol.Active.GetHashCode();
                if (symbol.Name != null)
                    hash = hash * 59 + symbol.Name.GetHashCode();
                if (symbol.Market != null)
                    hash = hash * 59 + symbol.Market.GetHashCode();
                hash = hash * 59 + symbol.Security.GetHashCode();
            }

            return hash;
        }
    }
}
