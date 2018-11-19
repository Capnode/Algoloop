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
using System;
using System.Data;
using System.Globalization;

namespace Algoloop.Service
{
    public static class Summary
    {
        public static DataRow CreateRow(this DataTable table, StrategyJobViewModel task)
        {
            DataRow row = null;

            // Find existing row
            foreach (DataRow item in table.Rows)
            {
                if (task.Equals(item[0]))
                {
                    row = item;
//                    row.Clear();
                    break;
                }
            }

            // Create new row
            if (row == null)
            {
                row = table.NewRow();
                table.Rows.Add(row);
            }

            if (!table.Columns.Contains("Task-name"))
            {
                var column = new DataColumn();
                column.ColumnName = "Task-name";
                column.DataType = typeof(StrategyJobViewModel);
                column.ReadOnly = false;
                column.Unique = false;
                table.Columns.Add(column);
            }

            if (!table.Columns.Contains("Task-active"))
            {
                var column = new DataColumn();
                column.ColumnName = "Task-active";
                column.DataType = typeof(bool);
                column.ReadOnly = false;
                column.Unique = false;
                table.Columns.Add(column);
            }

            row[0] = task;
            row[1] = task.Active;
            return row;
        }

        public static void Add(this DataTable table, DataRow row, string name, string text)
        {
            decimal value;
            if (text.Contains("$") && decimal.TryParse(text.Replace("$", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                string header = name + "%";
                if (!table.Columns.Contains(header))
                {
                    var column = new DataColumn();
                    column.ColumnName = header;
                    column.DataType = typeof(decimal);
                    column.ReadOnly = false;
                    column.Unique = false;
                    table.Columns.Add(column);
                }

                row[header] = value;
            }
            else if (text.Contains("%") && decimal.TryParse(text.Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                string header = name + "%";
                if (!table.Columns.Contains(header))
                {
                    var column = new DataColumn();
                    column.ColumnName = header;
                    column.DataType = typeof(decimal);
                    column.ReadOnly = false;
                    column.Unique = false;
                    table.Columns.Add(column);
                }

                row[header] = value;
            }
            else if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                if (!table.Columns.Contains(name))
                {
                    var column = new DataColumn();
                    column.ColumnName = name;
                    column.DataType = typeof(decimal);
                    column.ReadOnly = false;
                    column.Unique = false;
                    table.Columns.Add(column);
                }

                row[name] = value;
            }
            else if (bool.TryParse(text, out bool boolVal))
            {
                if (!table.Columns.Contains(name))
                {
                    var column = new DataColumn();
                    column.ColumnName = name;
                    column.DataType = typeof(bool);
                    column.ReadOnly = false;
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
                    column.ReadOnly = false;
                    column.Unique = false;
                    table.Columns.Add(column);
                }

                row[name] = value;
            }
        }

        // Clears the data from the row, setting all of the column values to a 
        // reasonable default value.
        public static void Clear(this DataRow row)
        {
            // for each column in the schema
            for (int i = 0; i < row.Table.Columns.Count; i++)
            {
                // get the column
                DataColumn column = row.Table.Columns[i];
                // if the column doesn't have a default value
                if (column.DefaultValue != null)
                {
                    // Based on the data type of the column, set an appropriate 
                    // default value. Since we're only dealing with intrinsic 
                    // types, we can derive a kind of shortcut for the type name,
                    // thus making our switch statemenbt a bit shorter.
                    switch (column.DataType.Name.ToLower().Substring(0, 3))
                    {
                        case "str":
                        case "cha":
                            row[i] = "";
                            break;
                        case "int":
                        case "uin":
                        case "sho":
                        case "byt":
                        case "sby":
                        case "dec":
                        case "dou":
                        case "sin":
                            row[i] = 0;
                            break;
                        case "boo":
                            row[i] = false;
                            break;
                        case "dat":
                            row[i] = new DateTime(0);
                            break;
                        case "obj":
                        default:
                            row[i] = DBNull.Value;
                            break;
                    }
                }
                // otherwise, set the column to its default value
                else
                {
                    row[i] = column.DefaultValue;
                }
            }
        }
    }
}
