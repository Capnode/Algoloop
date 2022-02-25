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
using QuantConnect.AlgorithmFactory;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Algoloop.Support
{
    public class AlgorithmNameConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var model = context.Instance as StrategyModel;
            string assemblyPath = MainService.FullExePath(model?.AlgorithmLocation);
            if (string.IsNullOrEmpty(assemblyPath)) return null;

            try
            {
                Assembly assembly = Assembly.LoadFrom(assemblyPath);

                //Get the list of extention classes in the library: 
                List<string> extended = Loader.GetExtendedTypeNames(assembly);
                List<string> list = assembly.ExportedTypes
                    .Where(m => extended.Contains(m.FullName))
                    .Select(m => m.Name)
                    .ToList();
                list.Sort();
                return new StandardValuesCollection(list);
            }
            catch (Exception)
            {
            }

            string algorithm = Path.GetFileNameWithoutExtension(assemblyPath);
            var algorithms = new List<string>() { algorithm };
            return new StandardValuesCollection(algorithms);
        }
    }
}
