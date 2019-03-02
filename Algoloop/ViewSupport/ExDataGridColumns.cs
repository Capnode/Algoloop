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
    public static class ExDataGridColumns
    {
        static Style rightCellStyle = new Style(typeof(TextBlock));
        static Style leftCellStyle = new Style(typeof(TextBlock));

        static ExDataGridColumns()
        {
            rightCellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
            leftCellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Left));
        }

        public static void AddPropertyColumns(ObservableCollection<DataGridColumn> columns, IDictionary<string, string> properties, string binding)
        {
            if (properties == null)
                return;

            foreach (var property in properties)
            {
                bool isNumber = decimal.TryParse(property.Value, out _);
                AddTextColumn(columns, property.Key, $"{binding}[{property.Key}]", isNumber);
            }
        }

        public static void AddTextColumn(ObservableCollection<DataGridColumn> columns, string header, string binding, bool isRightAligned)
        {
            DataGridColumn column = columns.FirstOrDefault(m => m.Header.Equals(header));
            if (column != null)
            {
                // Set leftCellStyle if not a number
                if (column is DataGridTextColumn textColumn)
                {
                    if (!isRightAligned && textColumn.ElementStyle.Equals(rightCellStyle))
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
                    Header = header,
                    IsReadOnly = true,
                    Binding = new Binding(binding)
                    {
                        Mode = BindingMode.OneWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        FallbackValue = ""
                    },
                    ElementStyle = isRightAligned ? rightCellStyle : leftCellStyle
                });
            }
        }
    }
}
