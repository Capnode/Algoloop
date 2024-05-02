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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;

namespace Algoloop.Wpf.ViewModels.Internal.Provider
{
    internal class Fxcm : ProviderBase
    {
        private FxcmClient _api;

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

        public override void GetUpdate(ProviderModel provider, Action<object> update, CancellationToken cancel)
        {
            //Log.Trace($">{GetType().Name}:GetUpdate");
            DateTime now = DateTime.UtcNow;
            Debug.WriteLine($"provider Active={provider.Symbols.Where(m => m.Active).Count()}");
            IReadOnlyList<SymbolModel> symbols = _api.GetSymbolsAsync().Result;
            Debug.WriteLine($"GetSymbols Active={symbols.Where(m => m.Active).Count()}");

            // Sync remote symbol Active property with local
            UpdateSymbols(provider, symbols, update);
            foreach (SymbolModel symbol in symbols)
            {
                SymbolModel item = provider.Symbols.FirstOrDefault(m => 
                    m.Name == symbol.Name && m.Active != symbol.Active);
                if (item == null) continue;
                _api.SubscribeOfferAsync(item).Wait();
            }

            // Get all account tables
            _api.GetAccountsAsync(update).Wait();

            //            provider.LastDate = now.ToLocalTime();
            //Log.Trace($"<{GetType().Name}:GetUpdate");
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _api?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
