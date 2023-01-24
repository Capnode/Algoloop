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
using Capnode.Wpf.DataGrid.Internal;

namespace Capnode.Wpf.DataGrid
{
    public partial class ColumnOptionControl : UserControl, INotifyPropertyChanged
    {
        private readonly FilterOperationItem _addPin = new(Enums.FilterOperation.Unknown, "Pin Column", "/Capnode.Wpf;component/Images/PinUp.png");
        private readonly FilterOperationItem _addGroup = new(Enums.FilterOperation.Unknown, "Add Grouping", "/Capnode.Wpf;component/Images/GroupBy.png");
        private readonly FilterOperationItem _removePin = new(Enums.FilterOperation.Unknown, "Unpin Column", "/Capnode.Wpf;component/Images/pinDown.png");
        private readonly FilterOperationItem _removeGroup = new(Enums.FilterOperation.Unknown, "Remove Grouping", "/Capnode.Wpf;component/Images/RemoveGroupBy.png");

        public ExDataGrid Grid { get; set; }

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

        public FilterOperationItem AddPin => _addPin;

        public ColumnOptionControl()
        {

            ColumnOptions = new ObservableCollection<FilterOperationItem>();
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = this;
                this.Loaded += new RoutedEventHandler(ColumnOptionControl_Loaded);
                cbOptions.DropDownOpened += new EventHandler(CbOptions_DropDownOpened);
            }
        }

        void CbOptions_DropDownOpened(object sender, EventArgs e)
        {
            ColumnOptions.Clear();
            if (CanUserFreeze)
            {
                if (Grid.IsFrozenColumn(FilterColumnInfo.Column))
                    ColumnOptions.Add(_removePin);
                else
                    ColumnOptions.Add(AddPin);
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
                colHeader ??= parent as DataGridColumnHeader;
                Grid ??= parent as ExDataGrid;
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

                    boundColumn.ElementStyle = CellStyle(TextAlignment.Right, boundColumn.ElementStyle);
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        #endregion

        internal void ResetVisibility()
        {
            if ((!CanUserGroup && !CanUserFreeze) || string.IsNullOrWhiteSpace(FilterColumnInfo.PropertyPath))
                this.Visibility = System.Windows.Visibility.Collapsed;
            else
                this.Visibility = System.Windows.Visibility.Visible;
        }

        private void CbOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private static Style CellStyle(TextAlignment alignment, Style basedOn)
        {
            var style = new Style(typeof(TextBlock), basedOn);
            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, alignment));
            return style;
        }
    }
}
