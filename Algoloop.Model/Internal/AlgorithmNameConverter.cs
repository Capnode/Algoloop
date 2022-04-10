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

using QuantConnect;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Algoloop.Model.Internal
{
    internal class AlgorithmNameConverter : TypeConverter
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
            string path = MainService.FullExePath(model?.AlgorithmLocation);
            return model.AlgorithmLanguage switch
            {
                Language.CSharp or Language.FSharp or Language.VisualBasic => ClrAlgorithm(path, model.AlgorithmName),
                Language.Python => PythonAlgorithm(path),
                _ => new StandardValuesCollection(new List<string>()),
            };
        }

        private static StandardValuesCollection ClrAlgorithm(string path, string name)
        {
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFile(path);

                    // Get the list of extention classes in the library: 
                    List<string> extended = Loader.GetExtendedTypeNames(assembly);
                    List<string> list = assembly.ExportedTypes
                        .Where(m => extended.Contains(m.FullName))
                        .Select(m => m.Name)
                        .ToList();
                    list.Sort();
                    return new StandardValuesCollection(list);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            return new StandardValuesCollection(new List<string>() { name });
        }

        private static StandardValuesCollection PythonAlgorithm(string path)
        {
            string algorithm = Path.GetFileNameWithoutExtension(path);
            return new StandardValuesCollection(new List<string>() { algorithm });
        }
    }
}
