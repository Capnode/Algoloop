/*
 * Copyright 2022 Capnode AB
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
using QuantConnect.Algorithm;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Algoloop.Model.Internal
{
    internal class AlgorithmFileConverter : TypeConverter
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
            StrategyModel model = context.Instance as StrategyModel ?? throw new ApplicationException(nameof(context.Instance));
            string folder = string.IsNullOrEmpty(model.AlgorithmFolder) ? MainService.GetProgramFolder() : model.AlgorithmFolder;
            switch (model.AlgorithmLanguage)
            {
                case Language.CSharp:
                case Language.FSharp:
                case Language.VisualBasic:
                    return ClrAlgorithm(folder);
                case Language.Python:
                    return PythonAlgorithm(folder);
                case Language.Java:
                default:
                    throw new NotImplementedException(model.AlgorithmLanguage.ToString());                        
            }
        }

        private StandardValuesCollection ClrAlgorithm(string folder)
        {
            List<string> list = new();
            List<string> paths = new();
            paths.AddRange(Directory.GetFiles(folder, "*.exe", SearchOption.TopDirectoryOnly));
            paths.AddRange(Directory.GetFiles(folder, "*.dll", SearchOption.TopDirectoryOnly));
            foreach (string path in paths)
            {
                try
                {
                    Assembly asm = Assembly.LoadFile(path);
                    if (asm.GetTypes().Any(p => typeof(QCAlgorithm).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract))
                    {
                        list.Add(Path.GetFileName(path));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            list.Sort();
            return new StandardValuesCollection(list);
        }

        private StandardValuesCollection PythonAlgorithm(string folder)
        {
            List<string> list = new();
            List<string> paths = new();
            paths.AddRange(Directory.GetFiles(folder, "*.py", SearchOption.TopDirectoryOnly));
            foreach (string path in paths)
            {
                list.Add(Path.GetFileName(path));
            }

            list.Sort();
            return new StandardValuesCollection(list);
        }
    }
}
