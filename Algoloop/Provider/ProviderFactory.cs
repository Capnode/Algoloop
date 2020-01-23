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
using Algoloop.Lean;
using Algoloop.Model;
using Algoloop.Service;
using Algoloop.ViewModel;
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
        public MarketModel Run(MarketModel market, SettingService settings, ILogHandler logger)
        {
            if (market == null) throw new ArgumentNullException(nameof(market));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            Log.LogHandler = logger;
            PrepareDataFolder(settings.DataFolder);

            using (var writer = new StreamLogger(logger))
            {
                Console.SetOut(writer);
                IList<string> symbols = market.Symbols.Select(m => m.Name).ToList();
                if (!symbols.Any())
                {
                    Log.Trace($"No symbols selected");
                    market.Active = false;
                    return market;
                }

                IProvider provider = CreateProvider(market.Provider);
                if (provider == null)
                {
                    market.Active = false;
                }

                try
                {
                    provider.Download(market, settings, symbols);
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

            Log.LogHandler.Dispose();
            return market;
        }

        public override object InitializeLifetimeService()
        {
            // No lifetime timeout
            return null;
        }

        public static IEnumerable<SymbolModel> GetAllSymbols(MarketModel market, SettingService settings)
        {
            if (market == null) throw new ArgumentNullException(nameof(market));

            IProvider provider = CreateProvider(market.Provider);
            if (provider == null)
                return null;

            return provider.GetAllSymbols(market, settings);
        }

        public static void RegisterProviders()
        {
            IEnumerable<Type> providers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IProvider).IsAssignableFrom(p) && !p.IsInterface);
            foreach (Type provider in providers)
            {
                RegisterProvider(provider);
            }
        }

        public static void PrepareDataFolder(string dataFolder)
        {
            Config.Set("data-directory", dataFolder);
            Config.Set("data-folder", dataFolder);
            Config.Set("cache-location", dataFolder);

            // Update data
            string sourceDir = Path.Combine(MainViewModel.GetProgramFolder(), "Data/ProgramData");
            MainViewModel.CopyDirectory(Path.Combine(sourceDir, "market-hours"), Path.Combine(dataFolder, "market-hours"), true);
            MainViewModel.CopyDirectory(Path.Combine(sourceDir, "symbol-properties"), Path.Combine(dataFolder, "symbol-properties"), true);
        }

        private static IProvider CreateProvider(string name)
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

            RegisterProvider(type);

            return provider;
        }

        private static void RegisterProvider(Type provider)
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
        }
    }
}
