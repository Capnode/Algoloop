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

using Algoloop.ViewModel;
using System.Data;
using System.Globalization;

namespace Algoloop.Service
{
    public static class Summary
    {
        public static DataRow CreateRow(this DataTable table, StrategyJobViewModel task)
        {
            DataRow row = table.NewRow();
            if (!table.Columns.Contains("Task-name"))
            {
                var column = new DataColumn();
                column.ColumnName = "Task-name";
                column.DataType = typeof(StrategyJobViewModel);
                column.ReadOnly = true;
                column.Unique = false;
                table.Columns.Add(column);
            }

            row[0] = task;
            return row;
        }

        public static void Add(this DataTable table, DataRow row, string name, string value)
        {
            if (value.Contains("$"))
            {
                value = value.Replace("$", "");
                decimal val = decimal.Parse(value, CultureInfo.InvariantCulture);
                string header = name + "$";
                if (!table.Columns.Contains(header))
                {
                    var column = new DataColumn();
                    column.ColumnName = header;
                    column.DataType = typeof(decimal);
                    column.ReadOnly = true;
                    column.Unique = false;
                    table.Columns.Add(column);
                }

                row[header] = val;
            }
            else if (value.Contains("%"))
            {
                value = value.Replace("%", "");
                decimal val = decimal.Parse(value, CultureInfo.InvariantCulture);
                string header = name + "%";
                if (!table.Columns.Contains(header))
                {
                    var column = new DataColumn();
                    column.ColumnName = header;
                    column.DataType = typeof(decimal);
                    column.ReadOnly = true;
                    column.Unique = false;
                    table.Columns.Add(column);
                }

                row[header] = val;
            }
            else if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decVal))
            {
                if (!table.Columns.Contains(name))
                {
                    var column = new DataColumn();
                    column.ColumnName = name;
                    column.DataType = typeof(decimal);
                    column.ReadOnly = true;
                    column.Unique = false;
                    table.Columns.Add(column);
                }

                row[name] = decVal;
            }
            else if (bool.TryParse(value, out bool boolVal))
            {
                if (!table.Columns.Contains(name))
                {
                    var column = new DataColumn();
                    column.ColumnName = name;
                    column.DataType = typeof(bool);
                    column.ReadOnly = true;
                    column.Unique = false;
                    table.Columns.Add(column);
                }

                row[name] = boolVal;
            }
            else
            {
                if (!table.Columns.Contains(name))
                {
                    var column = new DataColumn();
                    column.ColumnName = name;
                    column.DataType = typeof(string);
                    column.ReadOnly = true;
                    column.Unique = false;
                    table.Columns.Add(column);
                }

                row[name] = value;
            }
        }
    }
}
