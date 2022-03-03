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
using System.Collections;
using System.Diagnostics.Contracts;
using System.Windows.Data;

namespace Algoloop.Wpf.Internal
{
    public class CompositeCollectionConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Contract.Requires(values != null);
            var collection = new CompositeCollection();
            foreach (object item in values)
            {
                if (item is IEnumerable ienum)
                {
                    collection.Add(new CollectionContainer() { Collection = ienum });
                }
                else
                {
                    collection.Add(item);
                }
            }

            return collection;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
