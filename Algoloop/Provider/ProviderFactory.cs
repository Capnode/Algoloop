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

using Algoloop.Common;
using Algoloop.Model;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Algoloop.Provider
{
    public class ProviderFactory : MarshalByRefObject
    {
        public MarketModel Download(MarketModel market, SettingModel settings, ILogHandler logger)
        {
            if (market == null) throw new ArgumentNullException(nameof(market));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            Log.LogHandler = logger;
            Config.Set("data-directory", settings.DataFolder);
            Config.Set("data-folder", settings.DataFolder);
            Config.Set("cache-location", settings.DataFolder);

            using (var writer = new StreamLogger(logger))
            {
                Console.SetOut(writer);
                IProvider provider = CreateProvider(settings, market.Provider);
                if (provider == null)
                {
                    market.Active = false;
                }
                else
                {
                    try
                    {
                        // Check default values
                        if (string.IsNullOrEmpty(market.Market))
                        {
                            market.Market = market.Provider;
                        }

                        if (market.Security.Equals(SecurityType.Base))
                        {
                            market.Security = SecurityType.Equity;
                        }

                        provider.Download(market, settings);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}: {1}",
                            ex.GetType(),
                            ex.Message));
                        market.Active = false;
                    }
                }
            }

            Log.LogHandler.Dispose();
            return market;
        }

        public override object InitializeLifetimeService()
        {
            // No lifetime timeout
            return null;
        }

        public static void RegisterProviders(SettingModel settings)
        {
            IEnumerable<Type> providers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IProvider).IsAssignableFrom(p) && !p.IsInterface);
            foreach (Type type in providers)
            {
                IProvider provider = (IProvider)Activator.CreateInstance(type) ??
                    throw new ApplicationException($"Can not create provider {type.Name}");
                RegisterProvider(settings, provider);
            }
        }

        private static IProvider CreateProvider(SettingModel settings, string name)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IProvider).IsAssignableFrom(p) && !p.IsInterface)
                .FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
                throw new ApplicationException($"Provider {name} not found");

            IProvider provider = (IProvider)Activator.CreateInstance(type) ??
                throw new ApplicationException($"Can not create provider {name}");

            if (!RegisterProvider(settings, provider)) return null;
            return provider;
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
