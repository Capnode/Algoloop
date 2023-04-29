/*
 * Copyright 2023 Capnode AB
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

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Capnode.Wpf.Tester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private Data? _selected;
        private IList _selectedItems = new List<Data>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Columns = new()
            {
                new DataGridCheckBoxColumn() { Header = "Active", Binding = new Binding("Active") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged } },
                new DataGridTextColumn() { Header = "Name", Binding = new Binding("Name") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged } },
                new DataGridTextColumn() { Header = "Int", Binding = new Binding("IntValue") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged } },
                new DataGridTextColumn() { Header = "Float", Binding = new Binding("FloatValue") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged } },
                new DataGridTextColumn() { Header = "Double", Binding = new Binding("DoubleValue") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged } },
                new DataGridTextColumn() { Header = "Decimal", Binding = new Binding("DecimalValue") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged } },
            };

            Items = new()
            {
                new Data() { Name = "Data1", IntValue = 1, FloatValue = 1, DoubleValue = 1, DecimalValue = 1},
                new Data() { Name = "Data2", IntValue = 2, FloatValue = 2, DoubleValue = 2, DecimalValue = 2},
                new Data() { Name = "Data3", IntValue = 3, FloatValue = 3, DoubleValue = 3, DecimalValue = 3},
                new Data() { Name = "Data4", IntValue = 4, FloatValue = 4, DoubleValue = 4, DecimalValue = 4},
                new Data() { Name = "Data5", IntValue = 5, FloatValue = 5, DoubleValue = 5, DecimalValue = 5},
                new Data() { Name = "Data6", IntValue = 6, FloatValue = 6, DoubleValue = 6, DecimalValue = 6},
            };
        }

        public ObservableCollection<DataGridColumn> Columns { get;}
        public ObservableCollection<Data> Items { get; }

        public IList SelectedItems
        {
            get { return _selectedItems; }
            set
            {
                _selectedItems = value;
                OnPropertyChanged();
            }
        }


        public Data? Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                OnPropertyChanged();
            }
        }

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
