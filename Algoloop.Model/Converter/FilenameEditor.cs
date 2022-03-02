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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Algoloop.Model.Converter
{
    internal class FilenameEditor : ITypeEditor
    {
        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            if (propertyItem == null) throw new ArgumentNullException(nameof(propertyItem));

            Grid panel = new Grid();
            panel.ColumnDefinitions.Add(new ColumnDefinition());
            panel.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = GridLength.Auto
            });

            TextBox textBox = new TextBox();
            textBox.BorderBrush = textBox.Background;
            textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            textBox.IsEnabled = !propertyItem.IsReadOnly;
            panel.Children.Add(textBox);

            Binding binding = new Binding("Value")
            {
                Source = propertyItem,
                Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
            };
            
            //bind to the Value property of the PropertyItem
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, binding);

            if (!propertyItem.IsReadOnly)
            {
                Button button = new Button
                {
                    Content = "   . . .   ",
                    Tag = propertyItem
                };

                button.Click += Button_Click;
                Grid.SetColumn(button, 1);
                panel.Children.Add(button);
            }

            return panel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!(((Button)sender).Tag is PropertyItem item))
            {
                return;
            }

            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            string path = item.Value?.ToString();
            string folder;
            if (path == null)
            {
                folder = MainService.GetProgramFolder();
            }
            else
            {
                string fullPath = Path.GetFullPath(path);
                folder = Path.GetDirectoryName(fullPath);
            }

            openFileDialog.InitialDirectory = folder;
            if ((bool)openFileDialog.ShowDialog())
            {
                item.Value = openFileDialog.FileName;
            }
        }
    }
}
