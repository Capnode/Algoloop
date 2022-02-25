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
using System.Windows;
using System.Windows.Data;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Algoloop.
    Support
{
    public class DateEditor : ITypeEditor
    {
        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            if (propertyItem == null) throw new ArgumentNullException(nameof(propertyItem));

            DateTimePicker picker = new DateTimePicker
            {
                Format = DateTimeFormat.ShortDate,
                HorizontalAlignment = HorizontalAlignment.Left,
                IsEnabled = !propertyItem.IsReadOnly
            };

            //create the binding from the bound property item to the editor
            var binding = new Binding("Value")
            {
                Source = propertyItem,
                ValidatesOnExceptions = true,
                ValidatesOnDataErrors = true,
                Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
            }; //bind to the Value property of the PropertyItem
            BindingOperations.SetBinding(picker, DateTimePicker.ValueProperty, binding);
            return picker;
        }
    }
}
