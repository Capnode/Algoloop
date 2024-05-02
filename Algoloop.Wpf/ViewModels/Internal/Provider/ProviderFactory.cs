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

using QuantConnect.Securities;
using QuantConnect.Util;
using QuantConnect.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace Algoloop.Wpf.ViewModels.Internal.Provider
{
    internal class ProviderFactory
    {
        public static IProvider CreateProvider(string name)
        {
            try
            {
                Type type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => typeof(IProvider).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
                    .FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
                    throw new ApplicationException($"Provider {name} not found");

                IProvider provider = (IProvider)Activator.CreateInstance(type) ??
                    throw new ApplicationException($"Can not create provider {name}");
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
    }
}
