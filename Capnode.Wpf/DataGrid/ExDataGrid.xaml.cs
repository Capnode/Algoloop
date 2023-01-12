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
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Capnode.Wpf.DataGrid.Internal;

namespace Capnode.Wpf.DataGrid
{
    internal delegate void FilterChangedEventHandler(object sender, FilterChangedEventArgs e);
    internal delegate void CancelableFilterChangedEventHandler(object sender, CancelableFilterChangedEventArgs e);

    /// <summary>
    /// Interaction logic for ExDataGrid.xaml
    /// Based on JibGridWPF https://archive.codeplex.com/?p=jibgridwpf
    /// </summary>
    public partial class ExDataGrid : System.Windows.Controls.DataGrid, INotifyPropertyChanged
    {
        internal event CancelableFilterChangedEventHandler BeforeFilterChangedEventHandler;
        internal event FilterChangedEventHandler AfterFilterChanged;

        private readonly List<ColumnOptionControl> _optionControls = new List<ColumnOptionControl>();
        private readonly PropertyChangedEventHandler _filterHandler;

        public static readonly DependencyProperty ExItemsSourceProperty = DependencyProperty.Register(
            "ExItemsSource",
            typeof(IEnumerable),
            typeof(ExDataGrid),
            new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));

        public static readonly DependencyProperty ExColumnsProperty = DependencyProperty.Register(
            "ExColumns",
            typeof(ObservableCollection<DataGridColumn>),
            typeof(ExDataGrid),
            new PropertyMetadata(OnDataGridColumnsPropertyChanged));

        public static readonly DependencyProperty ExColumnsInfoProperty = DependencyProperty.Register(
            "ExColumnsInfo",
            typeof(string),
            typeof(ExDataGrid),
            new FrameworkPropertyMetadata { BindsTwoWayByDefault = true, DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

        public static readonly DependencyProperty ExSelectedItemsProperty = DependencyProperty.Register(
            "ExSelectedItems",
            typeof(IList),
            typeof(ExDataGrid),
            new FrameworkPropertyMetadata { BindsTwoWayByDefault = true, DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

        public ExDataGrid()
        {
            Filters = new List<ColumnFilterControl>();
            _filterHandler = new PropertyChangedEventHandler(Filter_PropertyChanged);
            InitializeComponent();

            Loaded += (object sender, RoutedEventArgs e) =>
            {
                ExDataGrid grid = sender as ExDataGrid;
                grid?.UpdateColumnsInfo();
            };

            SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                ExSelectedItems = base.SelectedItems;
            };
        }

        public IEnumerable ExItemsSource
        {
            get { return (IEnumerable)GetValue(ExItemsSourceProperty); }
            set { SetValue(ExItemsSourceProperty, value); }
        }

        public ObservableCollection<DataGridColumn> ExColumns
        {
            get { return (ObservableCollection<DataGridColumn>)base.GetValue(ExColumnsProperty); }
            set { base.SetValue(ExColumnsProperty, value); }
        }

        public string ExColumnsInfo
        {
            get { return (string)base.GetValue(ExColumnsInfoProperty); }
            set { base.SetValue(ExColumnsInfoProperty, value); }
        }

        public IList ExSelectedItems
        {
            get { return (IList)GetValue(ExSelectedItemsProperty); }
            set { SetValue(ExSelectedItemsProperty, value); }
        }

        protected bool IsResetting { get; set; }

        public List<ColumnFilterControl> Filters { get; set; }

        public Type FilterType { get; set; }

        protected ICollectionView CollectionView
        {
            get { return this.ItemsSource as ICollectionView; }
        }

        public static void OnItemsSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ExDataGrid g)
            {
                var list = (IEnumerable)e.NewValue;
                if (list == null)
                    return;

                var view = new CollectionViewSource
                {
                    Source = list
                };

                Type srcT = e.NewValue.GetType().GetInterfaces().First(i => i.Name.StartsWith("IEnumerable", StringComparison.OrdinalIgnoreCase));
                g.FilterType = srcT.GetGenericArguments().First();
                g.ItemsSource = CollectionViewSource.GetDefaultView(list);
                if (g.Filters != null)
                {
                    foreach (var filter in g.Filters)
                    {
                        filter.ResetControl();
                    }
                }
            }
        }

        private static void OnDataGridColumnsPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var context = source as ExDataGrid;

            if (e.OldValue is ObservableCollection<DataGridColumn> oldItems)
            {
                foreach (DataGridColumn one in oldItems)
                {
                    context.Columns.Remove(one);
                }

                oldItems.CollectionChanged -= context.CollectionChanged;
            }

            if (e.NewValue is ObservableCollection<DataGridColumn> newItems)
            {
                foreach (DataGridColumn one in newItems)
                {
                    context.Columns.Add(one);
                }

                // Update column order
                context.UpdateColumnsInfo();
                newItems.CollectionChanged += context.CollectionChanged;
            }
        }

        private void UpdateColumnsInfo()
        {
            if (string.IsNullOrEmpty(ExColumnsInfo)) return;
            string[] infos = ExColumnsInfo.Split(';');
            foreach (DataGridColumn column in Columns)
            {
                for (int i = 0; i < infos.Count(); i++)
                {
                    string header = infos[i];
                    if (column.Header.Equals(header))
                    {
                        if (column.DisplayIndex != i)
                        {
                            column.DisplayIndex = i;
                        }
                    }
                }
            }
        }

        protected override void OnColumnReordered(DataGridColumnEventArgs e)
        {
            base.OnColumnReordered(e);
            ExColumnsInfo = string.Join(";", Columns
                .OrderBy(x => x.DisplayIndex)
                .Select(m => m.Header));
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                        foreach (DataGridColumn one in e.NewItems)
                            Columns.Add(one);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                        foreach (DataGridColumn one in e.OldItems)
                            Columns.Remove(one);
                    break;

                case NotifyCollectionChangedAction.Move:
                    Columns.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Columns.Clear();
                    if (e.NewItems != null)
                        foreach (DataGridColumn one in e.NewItems)
                            Columns.Add(one);
                    break;
            }
        }

        #region Grouping Properties

        [Bindable(false)]
        [Category("Appearance")]
        [DefaultValue("False")]
        private bool _collapseLastGroup = false;
        public bool CollapseLastGroup
        {
            get { return _collapseLastGroup; }
            set
            {
                if (_collapseLastGroup != value)
                {
                    _collapseLastGroup = value;
                    OnPropertyChanged(nameof(CollapseLastGroup));
                }
            }
        }

        [Bindable(false)]
        [Category("Appearance")]
        [DefaultValue("False")]
        private bool _canUserGroup = false;
        public bool CanUserGroup
        {
            get { return _canUserGroup; }
            set
            {
                if (_canUserGroup != value)
                {
                    _canUserGroup = value;
                    OnPropertyChanged(nameof(CanUserGroup));
                    foreach (var optionControl in Filters)
                        optionControl.CanUserGroup = _canUserGroup;
                }
            }
        }

        #endregion Grouping Properties

        #region Freezing Properties

        [Bindable(false)]
        [Category("Appearance")]
        [DefaultValue("False")]
        private bool _canUserFreeze = false;
        public bool CanUserFreeze
        {
            get { return _canUserFreeze; }
            set
            {
                if (_canUserFreeze != value)
                {
                    _canUserFreeze = value;
                    OnPropertyChanged(nameof(CanUserFreeze));
                    foreach (var optionControl in Filters)
                        optionControl.CanUserFreeze = _canUserFreeze;
                }
            }
        }

        #endregion Freezing Properties

        #region Filter Properties

        [Bindable(false)]
        [Category("Appearance")]
        [DefaultValue("False")]
        private bool _canUserSelectDistinct = false;
        public bool CanUserSelectDistinct
        {
            get { return _canUserSelectDistinct; }
            set
            {
                if (_canUserSelectDistinct != value)
                {
                    _canUserSelectDistinct = value;
                    OnPropertyChanged(nameof(CanUserSelectDistinct));
                    foreach (var optionControl in Filters)
                        optionControl.CanUserSelectDistinct = _canUserSelectDistinct;
                }
            }
        }

        [Bindable(false)]
        [Category("Appearance")]
        [DefaultValue("True")]
        private bool _canUserFilter = true;
        public bool CanUserFilter
        {
            get { return _canUserFilter; }
            set
            {
                if (_canUserFilter != value)
                {
                    _canUserFilter = value;
                    OnPropertyChanged(nameof(CanUserFilter));
                    foreach (var optionControl in Filters)
                        optionControl.CanUserFilter = _canUserFilter;
                }
            }
        }
        #endregion Filter Properties

        /// <summary>
        /// Whenever any registered OptionControl raises the FilterChanged property changed event, we need to rebuild
        /// the new predicate used to filter the CollectionView.  Since Multiple Columns can have predicate we need to
        /// iterate over all registered OptionControls and get each predicate.
        /// </summary>
        /// <param name="sender">The object which has risen the event</param>
        /// <param name="e">The property which has been changed</param>
        void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FilterChanged")
            {
                Predicate<object> predicate = null;
                foreach (var filter in Filters)
                    if (filter.HasPredicate)
                        if (predicate == null)
                            predicate = filter.GeneratePredicate();
                        else
                            predicate = predicate.And(filter.GeneratePredicate());
                bool canContinue = true;
                var args = new CancelableFilterChangedEventArgs(predicate);
                if (BeforeFilterChangedEventHandler != null && !IsResetting)
                {
                    BeforeFilterChangedEventHandler(this, args);
                    canContinue = !args.Cancel;
                }
                if (canContinue)
                {
                    ListCollectionView view = CollectionViewSource.GetDefaultView(this.ItemsSource) as ListCollectionView;
                    if (view != null && view.IsEditingItem)
                        view.CommitEdit();
                    if (view != null && view.IsAddingNew)
                        view.CommitNew();
                    if (CollectionView != null)
                        CollectionView.Filter = predicate;
                    AfterFilterChanged?.Invoke(this, new FilterChangedEventArgs(predicate));
                }
                else
                {
                    IsResetting = true;
                    var ctrl = sender as ColumnFilterControl;
                    ctrl.ResetControl();
                    IsResetting = false;
                }
            }
        }
        
        internal void RegisterOptionControl(ColumnFilterControl ctrl)
        {
            if (!Filters.Contains(ctrl))
            {
                ctrl.PropertyChanged += _filterHandler;
                Filters.Add(ctrl);
            }
        }


        #region Grouping

        public void AddGroup(string boundPropertyName)
        {
            if (!string.IsNullOrWhiteSpace(boundPropertyName) && CollectionView != null && CollectionView.GroupDescriptions != null)
            {
                foreach (var groupedCol in CollectionView.GroupDescriptions)
                {
                    if (groupedCol is PropertyGroupDescription propertyGroup && propertyGroup.PropertyName == boundPropertyName)
                        return;
                }

                CollectionView.GroupDescriptions.Add(new PropertyGroupDescription(boundPropertyName));
            }
        }

        public bool IsGrouped(string boundPropertyName)
        {
            if (CollectionView != null && CollectionView.Groups != null)
            {
                foreach (var g in CollectionView.GroupDescriptions)
                {
                    if (g is PropertyGroupDescription pgd)
                        if (pgd.PropertyName == boundPropertyName)
                            return true;
                }
            }

            return false;
        }

        public void RemoveGroup(string boundPropertyName)
        {
            if (!string.IsNullOrWhiteSpace(boundPropertyName) && CollectionView != null && CollectionView.GroupDescriptions != null)
            {
                PropertyGroupDescription selectedGroup = null;

                foreach (var groupedCol in CollectionView.GroupDescriptions)
                {
                    if (groupedCol is PropertyGroupDescription propertyGroup && propertyGroup.PropertyName == boundPropertyName)
                    {
                        selectedGroup = propertyGroup;
                    }
                }

                if (selectedGroup != null)
                    CollectionView.GroupDescriptions.Remove(selectedGroup);

                //if (CollapseLastGroup && CollectionView.Groups != null)
                    //foreach (CollectionViewGroup group in CollectionView.Groups)
                    //    RecursiveCollapse(group);
            }
        }

        public void ClearGroups()
        {
            if (CollectionView != null && CollectionView.GroupDescriptions != null)
                CollectionView.GroupDescriptions.Clear();
        }
        #endregion Grouping

        #region Freezing

        public void FreezeColumn(DataGridColumn column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));

            if (this.Columns != null && this.Columns.Contains(column))
            {
                column.DisplayIndex = this.FrozenColumnCount;
                this.FrozenColumnCount++;
            }
        }
        public bool IsFrozenColumn(DataGridColumn column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));

            if (this.Columns != null && this.Columns.Contains(column))
            {
                return column.DisplayIndex < this.FrozenColumnCount;
            }
            else
            {
                return false;
            }
        }
        public void UnFreezeColumn(DataGridColumn column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));

            if (this.FrozenColumnCount > 0 && column.IsFrozen && this.Columns != null && this.Columns.Contains(column))
            {
                this.FrozenColumnCount--;
                column.DisplayIndex = this.FrozenColumnCount;
            }
        }

        public void UnFreezeAllColumns()
        {
            for (int i = Columns.Count - 1; i >= 0; i--)
                UnFreezeColumn(Columns[i]);
        }

        #endregion Freezing

        public void ShowFilter(DataGridColumn column, Visibility visibility)
        {
            var ctrl = Filters.Where(i => i.FilterColumnInfo.Column == column).FirstOrDefault();
            if (ctrl != null)
                ctrl.FilterVisibility = visibility;
        }

        public void ConfigureFilter(DataGridColumn column, bool canUserSelectDistinct, bool canUserGroup, bool canUserFreeze, bool canUserFilter)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));

            column.SetValue(ColumnConfiguration.CanUserFilterProperty, canUserFilter);
            column.SetValue(ColumnConfiguration.CanUserFreezeProperty, canUserFreeze);
            column.SetValue(ColumnConfiguration.CanUserGroupProperty, canUserGroup);
            column.SetValue(ColumnConfiguration.CanUserSelectDistinctProperty, canUserSelectDistinct);
         
            var ctrl = Filters.Where(i => i.FilterColumnInfo.Column == column).FirstOrDefault();
            if (ctrl != null)
            {
                ctrl.CanUserSelectDistinct = canUserSelectDistinct;
                ctrl.CanUserGroup = canUserGroup;
                ctrl.CanUserFreeze = canUserFreeze;
                ctrl.CanUserFilter = canUserFilter;
            }
        }

        public void ResetDistinctLists()
        {
            foreach (var optionControl in Filters)
                optionControl.ResetDistinctList();
        }

        internal void RegisterColumnOptionControl(ColumnOptionControl columnOptionControl)
        {
            _optionControls.Add(columnOptionControl);
        }
        internal void UpdateColumnOptionControl(ColumnFilterControl columnFilterControl)
        {
            //Since visibility for column contrls is set off the ColumnFilterControl by the base grid, we need to 
            //update the ColumnOptionControl since it is a seperate object.
            var ctrl = _optionControls.Where(c => c.FilterColumnInfo != null && columnFilterControl.FilterColumnInfo != null && c.FilterColumnInfo.Column == columnFilterControl.FilterColumnInfo.Column).FirstOrDefault();
            if (ctrl != null)
                ctrl.ResetVisibility();
        }
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
