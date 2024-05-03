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

namespace Algoloop.Wpf.Model.Internal
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
                Language.CSharp or Language.FSharp or Language.VisualBasic => ClrAlgorithm(path),
                Language.Python => PythonAlgorithm(model?.AlgorithmFolder),
                _ => new StandardValuesCollection(new List<string>()),
            };
        }

        private static StandardValuesCollection ClrAlgorithm(string path)
        {
            List<string> list = new();
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFile(path);

                    // Get the list of extention classes in the library: 
                    List<string> extended = Loader.GetExtendedTypeNames(assembly);
                    List<string> strategies = assembly.ExportedTypes
                        .Where(m => extended.Contains(m.FullName))
                        .Select(m => m.Name)
                        .ToList();
                    list.AddRange(strategies);
                    list.Sort();
                    return new StandardValuesCollection(list);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            return new StandardValuesCollection(list);
        }

        private static StandardValuesCollection PythonAlgorithm(string folder)
        {
            List<string> list = new();
            if (!string.IsNullOrEmpty(folder))
            {
                string[] pyFiles = Directory.GetFiles(folder, "*.py");
                List<string> strategies = pyFiles.Select(Path.GetFileNameWithoutExtension).ToList();
                list.AddRange(strategies);
                list.Sort();
                return new StandardValuesCollection(list);
            }

            return new StandardValuesCollection(list);
        }
    }
}
