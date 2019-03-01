/*
 * Copyright 2019 Capnode AB
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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Algoloop.ViewSupport
{
    public class ExDataGridColumns
    {
        public static void AddPropertyColumns(ObservableCollection<DataGridColumn> columns, IDictionary<string, string> properties)
        {
            if (properties == null)
                return;

            Style rightCellStyle = new Style(typeof(TextBlock));
            rightCellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
            Style leftCellStyle = new Style(typeof(TextBlock));
            leftCellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Left));

            foreach (var property in properties)
            {
                bool isDecimal = decimal.TryParse(property.Value, out _);
                DataGridColumn column = columns.FirstOrDefault(m => m.Header.Equals(property.Key));
                if (column != null)
                {
                    // Set leftCellStyle if not a number
                    if (column is DataGridTextColumn textColumn)
                    {
                        if (!isDecimal && textColumn.ElementStyle.Equals(rightCellStyle))
                        {
                            textColumn.ElementStyle = leftCellStyle;
                        }
                    }
                }
                else
                {
                    // Create new DataGridColumn
                    columns.Add(new DataGridTextColumn()
                    {
                        Header = property.Key,
                        IsReadOnly = true,
                        Binding = new Binding($"Model.Properties[{property.Key}]")
                        {
                            Mode = BindingMode.OneWay,
                            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                            FallbackValue = string.Empty
                        },
                        ElementStyle = isDecimal ? rightCellStyle : leftCellStyle
                    });
                }
            }
        }
    }
}
