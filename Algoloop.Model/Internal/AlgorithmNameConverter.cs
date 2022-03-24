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

using QuantConnect.Algorithm;
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

            StrategyModel model = context.Instance as StrategyModel ?? throw new ArgumentNullException(nameof(model));
            string path = MainService.FullExePath(Path.Combine(model.AlgorithmFolder, model.AlgorithmFile));
            if (string.IsNullOrEmpty(path)) return null;

            List<string> list = new();
            try
            {
                Assembly asm = Assembly.LoadFile(path);
                var types = asm.GetTypes();

                foreach (Type strategy in asm.GetTypes().Select(m => m).Where(p => typeof(QCAlgorithm).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract))
                {
                    list.Add(Path.GetFileName(strategy.Name));
                }

                list.Sort();
                return new StandardValuesCollection(list);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            string algorithm = Path.GetFileNameWithoutExtension(path);
            var algorithms = new List<string>() { algorithm };
            return new StandardValuesCollection(algorithms);
        }
    }
}
