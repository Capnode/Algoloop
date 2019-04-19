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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls.Primitives;

namespace Algoloop.WPF.DataGrid
{
    public partial class ColumnOptionControl : UserControl, INotifyPropertyChanged
    {
        private static readonly Style _rightCellStyle = CellStyle(TextAlignment.Right);

        private FilterOperationItem _addPin = new FilterOperationItem(Enums.FilterOperation.Unknown, "Pin Column", "/Algoloop.WPF;component/Images/PinUp.png");
        private FilterOperationItem _addGroup = new FilterOperationItem(Enums.FilterOperation.Unknown, "Add Grouping", "/Algoloop.WPF;component/Images/GroupBy.png");
        private FilterOperationItem _removePin = new FilterOperationItem(Enums.FilterOperation.Unknown, "Unpin Column", "/Algoloop.WPF;component/Images/pinDown.png");
        private FilterOperationItem _removeGroup = new FilterOperationItem(Enums.FilterOperation.Unknown, "Remove Grouping", "/Algoloop.WPF;component/Images/RemoveGroupBy.png");

        public FilterDataGrid Grid { get; set; }

        public OptionColumnInfo FilterColumnInfo { get; set; }

        public ObservableCollection<FilterOperationItem> ColumnOptions { get; private set; }

        private FilterOperationItem _SelectedColumnOptionItem;
        public FilterOperationItem SelectedColumnOptionItem
        {
            get { return _SelectedColumnOptionItem; }

            set
            {
                if (_SelectedColumnOptionItem != value)
                {
                    _SelectedColumnOptionItem = value;
                    OnPropertyChanged("SelectedColumnOptionItem");
                }
            }
        }

        private bool _CanUserFreeze;
        public bool CanUserFreeze
        {
            get { return _CanUserFreeze; }
            set
            {
                if (value != _CanUserFreeze)
                {
                    _CanUserFreeze = value;
                    OnPropertyChanged("CanUserFreeze");
                }
            }
        }

        private bool _CanUserGroup;
        public bool CanUserGroup
        {
            get { return _CanUserGroup; }
            set
            {
                if (value != _CanUserGroup)
                {
                    _CanUserGroup = value;
                    OnPropertyChanged("CanUserGroup");
                }
            }
        }


        public ColumnOptionControl()
        {

            ColumnOptions = new ObservableCollection<FilterOperationItem>();
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = this;
                this.Loaded += new RoutedEventHandler(ColumnOptionControl_Loaded);
                cbOptions.DropDownOpened += new EventHandler(cbOptions_DropDownOpened);
            }
        }

        void cbOptions_DropDownOpened(object sender, EventArgs e)
        {
            ColumnOptions.Clear();
            if (CanUserFreeze)
            {
                if (Grid.IsFrozenColumn(FilterColumnInfo.Column))
                    ColumnOptions.Add(_removePin);
                else
                    ColumnOptions.Add(_addPin);
            }

            if (CanUserGroup)
            {
                if (Grid.IsGrouped(FilterColumnInfo.PropertyPath))
                    ColumnOptions.Add(_removeGroup);
                else
                    ColumnOptions.Add(_addGroup);
            }
        }

        void ColumnOptionControl_Loaded(object sender, RoutedEventArgs e)
        {
            //Check the Grid for Building commmands and Visibility
            DataGridColumn column = null;
            DataGridColumnHeader colHeader = null;

            UIElement parent = (UIElement)VisualTreeHelper.GetParent(this);
            while (parent != null)
            {
                parent = (UIElement)VisualTreeHelper.GetParent(parent);
                if (colHeader == null)
                    colHeader = parent as DataGridColumnHeader;

                if (Grid == null)
                    Grid = parent as FilterDataGrid;
            }

            if (colHeader != null)
            {
                column = colHeader.Column;
            }

            FilterColumnInfo = new OptionColumnInfo(column, Grid.FilterType);

            // Right adjust numeric column
            if (column is DataGridBoundColumn boundColumn)
            {
                if (TypeHelper.IsNumbericType(FilterColumnInfo.PropertyType))
                {
                    boundColumn.ElementStyle = _rightCellStyle;
                }
            }

            CanUserFreeze = Grid.CanUserFreeze;
            CanUserGroup = Grid.CanUserGroup;
            if (column != null)
            {
                object oCanUserFreeze = column.GetValue(ColumnConfiguration.CanUserFreezeProperty);
                if (oCanUserFreeze != null && (bool)oCanUserFreeze)
                    CanUserFreeze = (bool)oCanUserFreeze;

                object oCanUserGroup = column.GetValue(ColumnConfiguration.CanUserGroupProperty);
                if (oCanUserGroup != null && (bool)oCanUserGroup)
                    CanUserGroup = (bool)oCanUserGroup;
            }

            Grid.RegisterColumnOptionControl(this);
            ResetVisibility();
        }
        #region IPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        #endregion

        internal void ResetVisibility()
        {
            if ((!CanUserGroup && !CanUserFreeze) || string.IsNullOrWhiteSpace(FilterColumnInfo.PropertyPath))
                this.Visibility = System.Windows.Visibility.Collapsed;
            else
                this.Visibility = System.Windows.Visibility.Visible;
        }

        private void cbOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbOptions.IsDropDownOpen && SelectedColumnOptionItem != null)
            {
                switch (SelectedColumnOptionItem.Description)
                {
                    case "Pin Column":
                        Grid.FreezeColumn(FilterColumnInfo.Column);
                        break;
                    case "Add Grouping":
                        if (!string.IsNullOrWhiteSpace(FilterColumnInfo.PropertyPath))
                            Grid.AddGroup(FilterColumnInfo.PropertyPath);
                        break;
                    case "Unpin Column":
                        Grid.UnFreezeColumn(FilterColumnInfo.Column);
                        break;
                    case "Remove Grouping":
                        if (!string.IsNullOrWhiteSpace(FilterColumnInfo.PropertyPath))
                            Grid.RemoveGroup(FilterColumnInfo.PropertyPath);
                        break;
                }
                cbOptions.IsDropDownOpen = false;
            }
        }

        private static Style CellStyle(TextAlignment alignment)
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, alignment));
            return style;
        }
    }
}
