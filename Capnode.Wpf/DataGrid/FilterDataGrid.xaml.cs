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

namespace Capnode.Wpf.DataGrid
{
    public delegate void FilterChangedEvent(object sender, FilterChangedEventArgs e);
    public delegate void CancelableFilterChangedEvent(object sender, CancelableFilterChangedEventArgs e);

    /// <summary>
    /// Interaction logic for FilterDataGrid.xaml
    /// Based on JibGridWPF https://archive.codeplex.com/?p=jibgridwpf
    /// </summary>
    public partial class FilterDataGrid : System.Windows.Controls.DataGrid, INotifyPropertyChanged
    {
        public event CancelableFilterChangedEvent BeforeFilterChanged;
        public event FilterChangedEvent AfterFilterChanged;

        private List<ColumnOptionControl> _optionControls = new List<ColumnOptionControl>();
        private PropertyChangedEventHandler _filterHandler;

        public static readonly DependencyProperty ExItemsSourceProperty = DependencyProperty.Register(
            "ExItemsSource",
            typeof(IEnumerable),
            typeof(FilterDataGrid),
            new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));

        public static readonly DependencyProperty ExColumnsProperty = DependencyProperty.Register(
            "ExColumns",
            typeof(ObservableCollection<DataGridColumn>),
            typeof(FilterDataGrid),
            new PropertyMetadata(OnDataGridColumnsPropertyChanged));

        public static readonly DependencyProperty ExSelectedItemsProperty = DependencyProperty.Register(
            "ExSelectedItems",
            typeof(IList),
            typeof(FilterDataGrid),
            new FrameworkPropertyMetadata { BindsTwoWayByDefault = true, DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

        public FilterDataGrid()
        {
            Filters = new List<ColumnFilterControl>();
            _filterHandler = new PropertyChangedEventHandler(filter_PropertyChanged);
            InitializeComponent();

            SelectionChanged += (object sender, SelectionChangedEventArgs e) => { ExSelectedItems = base.SelectedItems; };
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

        public IList ExSelectedItems
        {
            get { return (IList)GetValue(ExSelectedItemsProperty); }
            set { SetValue(ExSelectedItemsProperty, value); }
        }

        public static void OnItemsSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            FilterDataGrid g = sender as FilterDataGrid;
            if (g != null)
            {
                var list = (IEnumerable)e.NewValue;
                var view = new CollectionViewSource();
                view.Source = list;
                if (list == null)
                    return;

                Type srcT = e.NewValue.GetType().GetInterfaces().First(i => i.Name.StartsWith("IEnumerable"));
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

        protected bool IsResetting { get; set; }

        public List<ColumnFilterControl> Filters { get; set; }

        public Type FilterType { get; set; }

        protected ICollectionView CollectionView
        {
            get { return this.ItemsSource as ICollectionView; }
        }

        private static void OnDataGridColumnsPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var context = source as FilterDataGrid;

            var oldItems = e.OldValue as ObservableCollection<DataGridColumn>;

            if (oldItems != null)
            {
                foreach (var one in oldItems)
                    context.Columns.Remove(one);

                oldItems.CollectionChanged -= context.collectionChanged;
            }

            var newItems = e.NewValue as ObservableCollection<DataGridColumn>;

            if (newItems != null)
            {
                foreach (var one in newItems)
                    context.Columns.Add(one);

                newItems.CollectionChanged += context.collectionChanged;
            }
        }

        private void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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
                    OnPropertyChanged("CollapseLastGroup");
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
                    OnPropertyChanged("CanUserGroup");
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
                    OnPropertyChanged("CanUserFreeze");
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
                    OnPropertyChanged("CanUserSelectDistinct");
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
                    OnPropertyChanged("CanUserFilter");
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
        void filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
                if (BeforeFilterChanged != null && !IsResetting)
                {
                    BeforeFilterChanged(this, args);
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
                    if (AfterFilterChanged != null)
                        AfterFilterChanged(this, new FilterChangedEventArgs(predicate));
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
                    var propertyGroup = groupedCol as PropertyGroupDescription;

                    if (propertyGroup != null && propertyGroup.PropertyName == boundPropertyName)
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
                    var pgd = g as PropertyGroupDescription;

                    if (pgd != null)
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
                    var propertyGroup = groupedCol as PropertyGroupDescription;

                    if (propertyGroup != null && propertyGroup.PropertyName == boundPropertyName)
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
            if (this.Columns != null && this.Columns.Contains(column))
            {
                column.DisplayIndex = this.FrozenColumnCount;
                this.FrozenColumnCount++;
            }
        }
        public bool IsFrozenColumn(DataGridColumn column)
        {
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
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
