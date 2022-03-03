/*
 * Copyright 2020 Capnode AB
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

using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Windows.Data;

namespace Algoloop.Wpf.Internal
{
    public class StringSumConverter : BaseConverter, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Contract.Requires(values != null);

            string name = null;
            decimal sum = 0M;
            foreach (object value in values)
            {
                if (value is string str)
                {
                    name = str;
                }
                else if (value is int num)
                {
                    sum += num;
                }
            }

            var format = string.Format(CultureInfo.InvariantCulture, (string)parameter, name, sum);
            return format;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            Contract.Requires(value != null);
            string[] splitValues = ((string)value).Split(' ');
            return splitValues;
        }
    }
}
