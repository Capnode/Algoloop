/*
 * Copyright 2021 Capnode AB
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
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Algoloop.Wpf.ViewSupport
{
    [ValueConversion(typeof(object), typeof(Brush))]
    public class ToBrushConverter : BaseConverter, IValueConverter
    {
        // https://spsexton.files.wordpress.com/2011/02/001-allcolors.png

        private readonly static Color _up = Colors.LightGreen;
        private readonly static Color _down = Colors.Salmon;
        private readonly static Color _none = Colors.Transparent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // The Brush is consumed when used and must be created each time
            if (value is decimal decval)
            {
                if (decval > 0) return new SolidColorBrush(_up);
                if (decval < 0) return new SolidColorBrush(_down);
            }
            else if (value is int intval)
            {
                if (intval > 0) return new SolidColorBrush(_up);
                if (intval < 0) return new SolidColorBrush(_down);
            }
            else if (value is bool boolval)
            {
                if (boolval) return new SolidColorBrush(_up);
                return new SolidColorBrush(_down);
            }

            return new SolidColorBrush(_none);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(nameof(ConvertBack));
        }
    }
}
