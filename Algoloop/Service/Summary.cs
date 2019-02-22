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
            DataRow row = null;

            // Find existing row
            foreach (DataRow item in table.Rows)
            {
                if (task.Equals(item[0]))
                {
                    row = item;
                    row.Clear();
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

            if (!table.Columns.Contains("Profit-DD"))
            {
                var column = new DataColumn();
                column.ColumnName = "Profit-DD";
                column.DataType = typeof(decimal);
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
                string header = name + "$";
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
                DataColumn column = row.Table.Columns[i];
                row[i] = column.DefaultValue;
            }
        }

        public static void RemoveUnusedColumns(this DataTable table)
        {
            // for each column in the schema
            for (int c = table.Columns.Count - 1; c >= 0; c--)
            {
                DataColumn column = table.Columns[c];
                bool clean = true;

                for (int r = 0; r < table.Rows.Count; r++)
                {
                    object cell = table.Rows[r][c];
                    if (!cell.Equals(column.DefaultValue))
                    {
                        clean = false;
                        break;
                    }
                }

                if (clean)
                {
                    table.Columns.Remove(column);
                }
            }
        }
    }
}
