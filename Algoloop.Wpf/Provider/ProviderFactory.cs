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

using Algoloop.Model;
using QuantConnect;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Algoloop.Provider
{
    public static class ProviderFactory
    {
        public static IProvider CreateProvider(MarketModel market, SettingModel settings)
        {
            if (market == null) throw new ArgumentNullException(nameof(market));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            string name = market.Provider;
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IProvider).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
                .FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
                throw new ApplicationException($"Provider {name} not found");

            IProvider provider = (IProvider)Activator.CreateInstance(type) ??
                throw new ApplicationException($"Can not create provider {name}");

            if (!RegisterProvider(settings, provider)) return null;
            return provider;
        }

        public static void RegisterProviders(SettingModel settings)
        {
            Contract.Requires(settings != null);

            IEnumerable<Type> providers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IProvider).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
            foreach (Type type in providers)
            {
                IProvider provider = (IProvider)Activator.CreateInstance(type) ??
                    throw new ApplicationException($"Can not create provider {type.Name}");
                RegisterProvider(settings, provider);
            }
        }

        private static bool RegisterProvider(SettingModel settings, IProvider provider)
        {
            string name = provider.GetType().Name.ToLowerInvariant();
            if (Market.Encode(name) == null)
            {
                // be sure to add a reference to the unknown market, otherwise we won't be able to decode it coming out
                int code = 0;
                while (Market.Decode(code) != null)
                {
                    code++;
                }

                Market.Add(name, code);
            }

            provider.Register(settings);
            return true;
        }
    }
}
