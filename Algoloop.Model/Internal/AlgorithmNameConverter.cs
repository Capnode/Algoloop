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
using QuantConnect.Algorithm;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

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
            StrategyModel model = context.Instance as StrategyModel ?? throw new ApplicationException(nameof(context.Instance));
            string filename = model.AlgorithmFile;
            switch (model.AlgorithmLanguage)
            {
                case Language.CSharp:
                case Language.FSharp:
                case Language.VisualBasic:
                    return ClrAlgorithm(filename);
                case Language.Python:
                    return PythonAlgorithm(filename);
                case Language.Java:
                default:
                    return new StandardValuesCollection(new List<string>());
            }
        }

        private StandardValuesCollection ClrAlgorithm(string filename)
        {
            IEnumerable<Type> types = Composer.Instance.GetExportedTypes<QCAlgorithm>();
            List<string> list = types
                .Where(t => t.Module.Name == filename)
                .Select(m => m.Name)
                .Distinct()
                .OrderBy(m => m)
                .ToList();
            return new StandardValuesCollection(list);
        }

        private StandardValuesCollection PythonAlgorithm(string filename)
        {
            List<string> list = new () { Path.GetFileNameWithoutExtension(filename) };
            return new StandardValuesCollection(list);
        }
    }
}
