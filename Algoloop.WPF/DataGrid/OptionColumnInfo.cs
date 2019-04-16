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

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Algoloop.WPF.DataGrid
{
    public class OptionColumnInfo
    {
        private static readonly Style _rightCellStyle = CellStyle(TextAlignment.Right);

        public DataGridColumn Column { get; set; }
        public bool IsValid { get; set; }
        public string PropertyPath { get; set; }
        public IValueConverter Converter { get; set; }
        public object ConverterParameter { get; set; }
        public System.Globalization.CultureInfo ConverterCultureInfo { get; set; }
        public Type PropertyType { get; set; }

        public OptionColumnInfo(DataGridColumn column, Type boundObjectType)
        {
            if (column == null)
                return;

            Column = column;
            if (column is DataGridBoundColumn boundColumn)
            {
                if (boundColumn.Binding is Binding binding
                    && !string.IsNullOrWhiteSpace(binding.Path.Path))
                {
                    System.Reflection.PropertyInfo propInfo = null;
                    if (boundObjectType != null)
                        propInfo = boundObjectType.GetProperty(binding.Path.Path);

                    if (propInfo != null)
                    {
                        IsValid = true;
                        PropertyPath = binding.Path.Path;
                        PropertyType = propInfo != null ? propInfo.PropertyType : typeof(string);
                        Converter = binding.Converter;
                        ConverterCultureInfo = binding.ConverterCulture;
                        ConverterParameter = binding.ConverterParameter;
                        if (TypeHelper.IsNumbericType(PropertyType))
                        {
                            boundColumn.ElementStyle = _rightCellStyle;
                        }
                    }
                    else
                    {
                        if (Debugger.IsAttached && Debugger.IsLogging())
                        {
                            Debug.WriteLine("Algoloop.WPF.DataGrid.FilterGrid: BindingExpression path error: '{0}' property not found on '{1}'", binding.Path.Path, boundObjectType.ToString());
                        }
                    }
                }
            }
            else if (column.SortMemberPath != null && column.SortMemberPath.Length > 0)
            {
                PropertyPath = column.SortMemberPath;
                PropertyType = boundObjectType.GetProperty(column.SortMemberPath).PropertyType;
            }
        }

        private static Style CellStyle(TextAlignment alignment)
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, alignment));
            return style;
        }

        public override string ToString()
        {
            if (PropertyPath != null)
                return PropertyPath;
            else
                return string.Empty;
        }
    }
}
