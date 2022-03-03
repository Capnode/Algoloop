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
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Data;

namespace Capnode.Wpf.DataGrid
{
    public class OptionColumnInfo
    {
        public DataGridColumn Column { get; set; }
        public bool IsValid { get; set; }
        public string PropertyPath { get; set; }
        public IValueConverter Converter { get; set; }
        public object ConverterParameter { get; set; }
        public System.Globalization.CultureInfo ConverterCultureInfo { get; set; }
        public Type PropertyType { get; set; }

        public OptionColumnInfo(DataGridColumn column, Type boundObjectType)
        {
            if (column == null) return;
            if (boundObjectType == null) throw new ArgumentNullException(nameof(boundObjectType));

            Column = column;
            if (column is DataGridBoundColumn boundColumn)
            {
                if (boundColumn.Binding is Binding binding
                    && !string.IsNullOrWhiteSpace(binding.Path.Path))
                {
                    System.Reflection.PropertyInfo propInfo = null;
                    if (boundObjectType != null)
                    {
                        propInfo = GetProperty(boundObjectType, binding.Path.Path);
                    }

                    if (propInfo != null)
                    {
                        IsValid = true;
                        PropertyPath = binding.Path.Path;
                        PropertyType = propInfo != null ? propInfo.PropertyType : typeof(string);
                        Converter = binding.Converter;
                        ConverterCultureInfo = binding.ConverterCulture;
                        ConverterParameter = binding.ConverterParameter;
                    }
                    else
                    {
                        if (Debugger.IsAttached && Debugger.IsLogging())
                        {
                            Debug.WriteLine("Capnode.Wpf.DataGrid.ExDataGrid: BindingExpression path error: '{0}' property not found on '{1}'", binding.Path.Path, boundObjectType?.ToString());
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

        public override string ToString()
        {
            if (PropertyPath != null)
                return PropertyPath;
            else
                return string.Empty;
        }

        public static PropertyInfo GetProperty(Type baseType, string propertyName)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            string[] parts = propertyName.Split('.');
            if (parts.Length > 1)
            {
                return GetProperty(baseType.GetProperty(parts[0]).PropertyType, parts.Skip(1).Aggregate((a, i) => a + "." + i));
            }

            Match match = Regex.Match(propertyName, @"(.*?)\[(.*?)\]");
            if (match.Success)
            {
                string collection = match.Groups[1].Value;
                string member = match.Groups[2].Value;

                PropertyInfo nestedProperty = baseType.GetProperty(collection);
                DefaultMemberAttribute defaultMember = (DefaultMemberAttribute)Attribute.GetCustomAttribute(nestedProperty.PropertyType, typeof(DefaultMemberAttribute));
                PropertyInfo nestedIndexer = nestedProperty.PropertyType.GetProperty(defaultMember.MemberName);
                return nestedIndexer;
            }

            return baseType.GetProperty(propertyName);
        }
    }
}
