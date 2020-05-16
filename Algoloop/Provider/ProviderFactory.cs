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
using Algoloop.Service;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            PrepareDataFolder(settings.DataFolder);

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
            foreach (Type provider in providers)
            {
                RegisterProvider(settings, provider);
            }
        }

        public static void PrepareDataFolder(string dataFolder)
        {
            Config.Set("data-directory", dataFolder);
            Config.Set("data-folder", dataFolder);
            Config.Set("cache-location", dataFolder);

            // Update data
            string sourceDir = Path.Combine(MainService.GetProgramFolder(), "Data/ProgramData");
            MainService.CopyDirectory(Path.Combine(sourceDir, "market-hours"), Path.Combine(dataFolder, "market-hours"), true);
            MainService.CopyDirectory(Path.Combine(sourceDir, "symbol-properties"), Path.Combine(dataFolder, "symbol-properties"), true);
        }

        private static IProvider CreateProvider(SettingModel settings, string name)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IProvider).IsAssignableFrom(p) && !p.IsInterface)
                .FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (type == null)
            {
                Log.Trace($"Provider {name} not found");
                return null;
            }

            IProvider provider = (IProvider)Activator.CreateInstance(type);
            if (provider == null)
            {
                Log.Trace($"Can not create provider {name}");
                return null;
            }

            if (!RegisterProvider(settings, type)) return null;
            return provider;
        }

        private static bool RegisterProvider(SettingModel settings, Type provider)
        {
            string name = provider.Name.ToLowerInvariant();
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

            return true;
        }
    }
}
