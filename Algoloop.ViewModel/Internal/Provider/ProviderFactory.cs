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
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Securities;
using QuantConnect.Util;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Algoloop.ViewModel.Internal.Provider
{
    internal class ProviderFactory
    {
        public static IProvider CreateProvider(string name, SettingModel settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            try
            {
                Type type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => typeof(IProvider).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
                    .FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
                    throw new ApplicationException($"Provider {name} not found");

                IProvider provider = (IProvider)Activator.CreateInstance(type) ??
                    throw new ApplicationException($"Can not create provider {name}");
                if (!provider.Register(settings)) return null;
                return provider;
            }
            catch (ReflectionTypeLoadException ex)
            {
                Log.Error($"{ex.GetType()} {ex.Message}: {name}", true);
                foreach (Exception exception in ex.LoaderExceptions)
                {
                    Log.Error($"LoaderExceptions: {exception.Message}", true);
                }

                throw;
            }
        }

        public static void RegisterProviders(SettingModel settings)
        {
            Contract.Requires(settings != null);

            MarketHoursDatabase.Reset();
            MarketHoursDatabase marketHours = MarketHoursDatabase.FromDataFolder(settings.DataFolder);
            string originalJson = JsonConvert.SerializeObject(marketHours);

            // Try to register providers to Market
            Market.Reset();
            IEnumerable<Type> providers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IProvider).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
            foreach (Type type in providers)
            {
                IProvider provider = (IProvider)Activator.CreateInstance(type) ??
                    throw new ApplicationException($"Can not create provider {type.Name}");
                provider.Register(settings);
            }

            // Save market hours database if changed
            string json = JsonConvert.SerializeObject(marketHours);
            if (!json.Equals(originalJson, StringComparison.OrdinalIgnoreCase))
            {
                string path = Path.Combine(settings.DataFolder, "market-hours", "market-hours-database.json");
                File.WriteAllText(path, json);
            }
        }
    }
}
