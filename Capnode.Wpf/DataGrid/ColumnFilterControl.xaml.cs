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
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Controls.Primitives;
using System.Text.RegularExpressions;
using System.Windows;
using linq = System.Linq.Expressions;
using System.Globalization;
using Capnode.Wpf.DataGrid.Internal;

namespace Capnode.Wpf.DataGrid
{
    /// <summary>
    /// Interaction logic for ColumnFilterControl.xaml
    /// </summary>
    public partial class ColumnFilterControl : UserControl, INotifyPropertyChanged
    {
        private Func<object, object> _boundColumnPropertyAccessor = null;

        #region Properties

        public ObservableCollection<FilterOperationItem> FilterOperations { get; set; }

        public ObservableCollection<CheckboxComboItem> DistinctPropertyValues { get; set; }

        public bool HasPredicate { get { return FilterText.Length > 0 || DistinctPropertyValues.Where(d => d.IsChecked).Count() > 0; } }

        public OptionColumnInfo FilterColumnInfo { get; set; }

        public ExDataGrid Grid { get; set; }

        private bool _CanUserFreeze = true;
        public bool CanUserFreeze
        {
            get
            {
                return _CanUserFreeze;
            }
            set
            {
                _CanUserFreeze = value;
                Grid.UpdateColumnOptionControl(this);
                OnPropertyChanged("CanUserFreeze");
            }
        }

        private bool _CanUserGroup;
        public bool CanUserGroup
        {
            get
            {
                return _CanUserGroup;
            }
            set
            {
                _CanUserGroup = value;
                Grid.UpdateColumnOptionControl(this);
                OnPropertyChanged("CanUserGroup");
            }
        }

        private bool _CanUserFilter = true;
        public bool CanUserFilter
        {
            get
            {
                return _CanUserFilter;
            }
            set
            {
                _CanUserFilter = value;
                CalcControlVisibility();
            }
        }

        private bool _CanUserSelectDistinct = false;
        public bool CanUserSelectDistinct
        {
            get
            {
                return _CanUserSelectDistinct;
            }
            set
            {
                _CanUserSelectDistinct = value;
                CalcControlVisibility();
            }
        }

        public Visibility FilterVisibility
        {
            get
            {
                return this.Visibility;
            }
            set
            {
                this.Visibility = value;
            }
        }

        public bool FilterReadOnly
        {
            get { return DistinctPropertyValues.Where(i => i.IsChecked).Count() > 0; }
        }

        public bool FilterOperationsEnabled
        {
            get { return DistinctPropertyValues.Where(i => i.IsChecked).Count() == 0; }
        }


        public Brush FilterBackGround
        {
            get
            {
                if (DistinctPropertyValues.Where(i => i.IsChecked).Count() > 0)
                    return SystemColors.ControlBrush;
                else
                    return Brushes.White;
            }
        }
        private string _FilterText = string.Empty;
        public string FilterText
        {
            get { return _FilterText; }
            set
            {
                if (value != _FilterText)
                {
                    _FilterText = value;
                    OnPropertyChanged("FilterText");
                    OnPropertyChanged("FilterChanged");
                }
            }
        }


        private FilterOperationItem _SelectedFilterOperation;
        internal FilterOperationItem SelectedFilterOperation
        {
            get
            {
                if (DistinctPropertyValues.Where(i => i.IsChecked).Count() > 0)
                    return FilterOperations.Where(f => f.FilterOption == Enums.FilterOperation.Equals).FirstOrDefault();
                return _SelectedFilterOperation;
            }
            set
            {
                if (value != _SelectedFilterOperation)
                {
                    _SelectedFilterOperation = value;
                    OnPropertyChanged("SelectedFilterOperation");
                    OnPropertyChanged("FilterChanged");
                }
            }
        }
        #endregion

        public ColumnFilterControl()
        {
            DistinctPropertyValues = new ObservableCollection<CheckboxComboItem>();
            FilterOperations = new ObservableCollection<FilterOperationItem>();
            InitializeComponent();



            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = this;
                this.Loaded += new RoutedEventHandler(ColumnFilterControl_Loaded);
            }
        }


        void ColumnFilterControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataGridColumn column = null;
            DataGridColumnHeader colHeader = null;

            UIElement parent = (UIElement)VisualTreeHelper.GetParent(this);
            while (parent != null)
            {
                parent = (UIElement)VisualTreeHelper.GetParent(parent);
                if (colHeader == null)
                    colHeader = parent as DataGridColumnHeader;

                if (Grid == null)
                    Grid = parent as ExDataGrid;
            }

            if (colHeader != null)
                column =  colHeader.Column;
        
            CanUserFilter = Grid.CanUserFilter;
            CanUserFreeze = Grid.CanUserFreeze;
            CanUserGroup = Grid.CanUserGroup;
            CanUserSelectDistinct = Grid.CanUserSelectDistinct;


            if (column != null)
            {
                object oCanUserFilter = column.GetValue(ColumnConfiguration.CanUserFilterProperty);
                if (oCanUserFilter != null)
                    CanUserFilter = (bool)oCanUserFilter;

                object oCanUserFreeze = column.GetValue(ColumnConfiguration.CanUserFreezeProperty);
                if (oCanUserFreeze != null)
                    CanUserFreeze = (bool)oCanUserFreeze;

                object oCanUserGroup = column.GetValue(ColumnConfiguration.CanUserGroupProperty);
                if (oCanUserGroup != null)
                    CanUserGroup = (bool)oCanUserGroup;

                object oCanUserSelectDistinct = column.GetValue(ColumnConfiguration.CanUserSelectDistinctProperty);
                if (oCanUserSelectDistinct != null)
                    CanUserSelectDistinct = (bool)oCanUserSelectDistinct;
            }


            if (Grid.FilterType == null)
                return;

            FilterColumnInfo = new OptionColumnInfo(column, Grid.FilterType);

            Grid.RegisterOptionControl(this);

            FilterOperations.Clear();
            if (FilterColumnInfo.PropertyType != null)
            {
                FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.Equals, "Equals", "/Capnode.Wpf;component/Images/Equal.png"));
                if (TypeHelper.IsStringType(FilterColumnInfo.PropertyType))
                {
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.Contains, "Contains", "/Capnode.Wpf;component/Images/Contains.png"));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.StartsWith, "Starts With", "/Capnode.Wpf;component/Images/StartsWith.png"));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.EndsWith, "Ends With", "/Capnode.Wpf;component/Images/EndsWith.png"));
                }

                if (TypeHelper.IsNumbericType(FilterColumnInfo.PropertyType))
                {
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.NotEquals, "Not Equal", "/Capnode.Wpf;component/Images/NotEqual.png"));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.GreaterThan, "Greater Than", "/Capnode.Wpf;component/Images/GreaterThan.png"));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.GreaterThanEqual, "Greater Than or Equal", "/Capnode.Wpf;component/Images/GreaterThanEqual.png"));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.LessThan, "Less Than", "/Capnode.Wpf;component/Images/LessThan.png"));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.LessThanEqual, "Less Than or Equal", "/Capnode.Wpf;component/Images/LessThanEqual.png"));
                }

                SelectedFilterOperation = FilterOperations[0];
            }

            if (FilterColumnInfo != null && FilterColumnInfo.IsValid)
            {
                foreach (var i in DistinctPropertyValues.Where(i => i.IsChecked))
                {
                    i.IsChecked = false;
                }

                DistinctPropertyValues.Clear();
                FilterText = string.Empty;
                _boundColumnPropertyAccessor = null;

                if (TypeHelper.IsNumbericType(FilterColumnInfo.PropertyType))
                {
                    CanUserSelectDistinct = false;
                    Visibility = Visibility.Visible;
                }
                else if (!string.IsNullOrWhiteSpace(FilterColumnInfo.PropertyPath))
                {
                    Visibility = Visibility.Visible;
                    ParameterExpression arg = linq.Expression.Parameter(typeof(object), "x");
                    linq.Expression expr = PropertyExpression(Grid.FilterType, FilterColumnInfo.PropertyPath, arg);
                    linq.Expression conversion = linq.Expression.Convert(expr, typeof(object));
                    _boundColumnPropertyAccessor = linq.Expression.Lambda<Func<object, object>>(conversion, arg).Compile();
                }
                else
                {
                    Visibility = Visibility.Collapsed;
                }

                object oDefaultFilter = column.GetValue(ColumnConfiguration.DefaultFilterProperty);
                if (oDefaultFilter != null)
                    FilterText = (string)oDefaultFilter;
            }

            CalcControlVisibility();          
        }

        private static linq.Expression PropertyExpression(Type baseType, string propertyName, ParameterExpression arg)
        {
            linq.Expression expr = linq.Expression.Convert(arg, baseType);
            var parts = propertyName.Split('.');
            foreach (string part in parts.Take(parts.Length - 1))
            {
                expr = linq.Expression.PropertyOrField(expr, part);
            }

            propertyName = parts.Last();
            Match match = Regex.Match(propertyName, @"(.*?)\[(.*?)\]");
            if (match.Success)
            {
                string collection = match.Groups[1].Value;
                string member = match.Groups[2].Value;

                expr = linq.Expression.Property(expr, collection);
                linq.Expression[] key = new linq.Expression[] { linq.Expression.Constant(member) };
                return linq.Expression.Property(expr, "Item", key);
            }

            return linq.Expression.PropertyOrField(expr, propertyName);
        }

        private void TxtFilter_Loaded(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).DataContext = this;
        }

        private void TxtFilter_KeyUp(object sender, KeyEventArgs e)
        {
            FilterText = ((TextBox)sender).Text;
        }

        public Predicate<object> GeneratePredicate()
        {
            Predicate<object> predicate = null;
            if (DistinctPropertyValues.Where(i => i.IsChecked).Count() > 0)
            {
                foreach (var item in DistinctPropertyValues.Where(i => i.IsChecked))
                {
                    if (predicate == null)
                        predicate = GenerateFilterPredicate(FilterColumnInfo.PropertyPath, item.Tag.ToString(), Grid.FilterType, FilterColumnInfo.PropertyType, SelectedFilterOperation);
                    else
                        predicate = predicate.Or(GenerateFilterPredicate(FilterColumnInfo.PropertyPath, item.Tag.ToString(), Grid.FilterType, FilterColumnInfo.PropertyType.UnderlyingSystemType, SelectedFilterOperation));
                }
            }
            else
            {
                predicate = GenerateFilterPredicate(FilterColumnInfo.PropertyPath, FilterText, Grid.FilterType, FilterColumnInfo.PropertyType.UnderlyingSystemType, SelectedFilterOperation);
            }
            return predicate;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        protected static Predicate<object> GenerateFilterPredicate(string propertyName, string filterValue, Type objType, Type propType, FilterOperationItem filterItem)
        {
            if (filterItem == null) return null;
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (filterValue == null) throw new ArgumentNullException(nameof(filterValue));
            if (objType == null) throw new ArgumentNullException(nameof(objType));

            ParameterExpression objParam = linq.Expression.Parameter(typeof(object), "x");
            linq.UnaryExpression param = objType.IsByRef ? 
                linq.Expression.TypeAs(objParam, objType) :
                linq.Expression.Convert(objParam, objType);

            linq.Expression prop = ExpressionProperty(propertyName, param);
            ConstantExpression val = linq.Expression.Constant(filterValue);

            switch (filterItem.FilterOption)
            {
                case Enums.FilterOperation.Contains:
                    return ExpressionHelper.GenerateGeneric(prop, val, propType, objParam, "Contains");
                case Enums.FilterOperation.EndsWith:
                    return ExpressionHelper.GenerateGeneric(prop, val, propType, objParam, "EndsWith");
                case Enums.FilterOperation.StartsWith:
                    return ExpressionHelper.GenerateGeneric(prop, val, propType, objParam, "StartsWith");
                case Enums.FilterOperation.Equals:
                    return ExpressionHelper.GenerateEquals(prop, filterValue, propType, objParam);
                case Enums.FilterOperation.NotEquals:
                    return ExpressionHelper.GenerateNotEquals(prop, filterValue, propType, objParam);
                case Enums.FilterOperation.GreaterThanEqual:
                    return ExpressionHelper.GenerateGreaterThanEqual(prop, filterValue, propType, objParam);
                case Enums.FilterOperation.LessThanEqual:
                    return ExpressionHelper.GenerateLessThanEqual(prop, filterValue, propType, objParam);
                case Enums.FilterOperation.GreaterThan:
                    return ExpressionHelper.GenerateGreaterThan(prop, filterValue, propType, objParam);
                case Enums.FilterOperation.LessThan:
                    return ExpressionHelper.GenerateLessThan(prop, filterValue, propType, objParam);
                default:
                    throw new ArgumentException("Could not decode Search Mode.  Did you add a new value to the enum, or send in Unknown?");
            }
        }

        private static linq.Expression ExpressionProperty(string propertyName, linq.Expression expr)
        {
            var parts = propertyName.Split('.');
            foreach (string part in parts.Take(parts.Length - 1))
            {
                expr = linq.Expression.PropertyOrField(expr, part);
            }

            propertyName = parts.Last();
            Match match = Regex.Match(propertyName, @"(.*?)\[(.*?)\]");
            if (match.Success)
            {
                string collection = match.Groups[1].Value;
                string member = match.Groups[2].Value;

                var prop = linq.Expression.Property(expr, collection);
                linq.Expression[] key = new linq.Expression[] { linq.Expression.Constant(member) };
                return linq.Expression.Property(prop, "Item", key);
            }

            return linq.Expression.Property(expr, propertyName);
        }

        public void ResetControl()
        {
            foreach (var i in DistinctPropertyValues)
                i.IsChecked = false;
            FilterText = string.Empty;

            DistinctPropertyValues.Clear();
        }
        public void ResetDistinctList()
        {
            DistinctPropertyValues.Clear();
        }
        private void CalcControlVisibility()
        {
            if (CanUserFilter)
            {
                cbOperation.Visibility = Visibility.Visible;
                if (CanUserSelectDistinct)
                {
                    cbDistinctProperties.Visibility = System.Windows.Visibility.Visible;
                    txtFilter.Visibility = Visibility.Collapsed;
                }
                else
                {
                    cbDistinctProperties.Visibility = System.Windows.Visibility.Collapsed;
                    txtFilter.Visibility = Visibility.Visible;
                }
            }
            else
            {
                cbOperation.Visibility = Visibility.Collapsed;
                cbDistinctProperties.Visibility = Visibility.Collapsed;
                txtFilter.Visibility = Visibility.Collapsed;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void CbDistinctProperties_DropDownOpened(object sender, EventArgs e)
        {
            if (_boundColumnPropertyAccessor == null)
                return;

            if (DistinctPropertyValues.Count == 0)
            {
                List<object> result = new List<object>();
                foreach (var i in Grid.ExItemsSource)
                {
                    object value = _boundColumnPropertyAccessor(i);
                    if (value != null)
                    {
                        if (result.Where(o => o.ToString() == value.ToString()).Count() == 0)
                        {
                            result.Add(value);
                        }
                    }
                }

                try
                {
                    result.Sort();
                }
                catch
                {
                    if (System.Diagnostics.Debugger.IsLogging())
                    {
                        System.Diagnostics.Debugger.Log(0, "Warning", "There is no default compare set for the object type");
                    }
                }

                foreach (var obj in result)
                {
                    var item = new CheckboxComboItem()
                    {
                        Description = GetFormattedValue(obj),
                        Tag = obj,
                        IsChecked = false
                    };
                    item.PropertyChanged += new PropertyChangedEventHandler(Filter_PropertyChanged);
                    DistinctPropertyValues.Add(item);
                }
            }
        }

        private string GetFormattedValue(object obj)
        {
            if (FilterColumnInfo.Converter != null)
                return FilterColumnInfo.Converter.Convert(obj, typeof(string), FilterColumnInfo.ConverterParameter, FilterColumnInfo.ConverterCultureInfo).ToString();
            else
                return obj.ToString();
        }

        void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var list = DistinctPropertyValues.Where(i => i.IsChecked).ToList();
            if (list.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var i in DistinctPropertyValues.Where(i => i.IsChecked))
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", sb.Length > 0 ? "," : "", i);
                }

                FilterText = sb.ToString();
            }
            else
            {
                FilterText = string.Empty;
            }
            OnPropertyChanged("FilterReadOnly");
            OnPropertyChanged("FilterBackGround");
            OnPropertyChanged("FilterOperationsEnabled");
            OnPropertyChanged("SelectedFilterOperation");
        }

        #region IPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        #endregion
    }
}
