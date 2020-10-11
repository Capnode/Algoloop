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

using System;
using System.Windows.Data;
using System.Windows.Input;

namespace Algoloop.ViewSupport
{
    /// <summary>
    /// Sets the cursor state of the mouse.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Cursors))]
    public class BusyMouseConverter : BaseConverter, IValueConverter
    {
        /// <summary>
        /// BusyMouseConverter constructor
        /// </summary>
        public BusyMouseConverter()
        {
        }

        /// <summary>
        /// Convert operation
        /// </summary>
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool)value)
                {
                    return Cursors.Wait;
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Convertback operation
        /// </summary>
        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is Cursors)
            {
                if (value == Cursors.Wait)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return null;
        }
    }
}
