using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Xml.Serialization;
using System.Diagnostics;
using System.IO;
using System.Collections;

namespace Algoloop.Wpf.Internal
{
    internal class WpDataGrid : DataGrid
    {
        private bool inWidthChange = false;
        private bool updatingColumnInfo = false;
        public static readonly DependencyProperty ColumnInformationProperty = DependencyProperty.Register("ColumnInformation", typeof(string), typeof(WpDataGrid),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ColumnInfoChangedCallback)
            );

        private static void ColumnInfoChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var grid = (WpDataGrid)dependencyObject;
            if (!grid.updatingColumnInfo)
            {
                grid.ColumnInfoChanged();
            }
        }

        public static readonly DependencyProperty SelectedItemsListProperty = DependencyProperty.Register("SelectedItemsList", typeof(IList), typeof(WpDataGrid),
            new PropertyMetadata(null));

        void CustomDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SelectedItemsList = this.SelectedItems;
        }

        public WpDataGrid()
        {
            this.SelectionChanged += CustomDataGrid_SelectionChanged;
        }

        protected override void OnInitialized(EventArgs e)
        {
            void sortDirectionChangedHandler(object sender, EventArgs x) => UpdateColumnInfo();
            void widthPropertyChangedHandler(object sender, EventArgs x) => inWidthChange = true;
            var sortDirectionPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.SortDirectionProperty, typeof(DataGridColumn));
            var widthPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));

            Loaded += (sender, x) =>
            {
                ColumnInfoChanged();
                foreach (var column in Columns)
                {
                    sortDirectionPropertyDescriptor.AddValueChanged(column, sortDirectionChangedHandler);
                    widthPropertyDescriptor.AddValueChanged(column, widthPropertyChangedHandler);
                }
            };
            Unloaded += (sender, x) =>
            {
                foreach (var column in Columns)
                {
                    sortDirectionPropertyDescriptor.RemoveValueChanged(column, sortDirectionChangedHandler);
                    widthPropertyDescriptor.RemoveValueChanged(column, widthPropertyChangedHandler);
                }
            };

            // Set columns chooser
            base.ContextMenu = new ContextMenu();
            base.ContextMenuOpening += ContextMenuOpening;

            base.OnInitialized(e);
        }

        private new void ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            base.ContextMenu.Items.Clear();
            foreach (var column in base.Columns)
            {
                MenuItem item = new()
                {
                    Header = column.Header,
                    IsCheckable = true,
                    IsChecked = column.Visibility.Equals(Visibility.Visible)
                };

                item.Checked += new RoutedEventHandler(MenuItem_CheckedChanged);
                item.Unchecked += new RoutedEventHandler(MenuItem_CheckedChanged);
                base.ContextMenu.Items.Add(item);
            }
        }

        public string ColumnInformation
        {
            get { return (string)GetValue(ColumnInformationProperty); }
            set { SetValue(ColumnInformationProperty, value); }
        }

        public IList SelectedItemsList
        {
            get { return (IList)GetValue(SelectedItemsListProperty); }
            set { SetValue(SelectedItemsListProperty, value); }
        }

        private void MenuItem_CheckedChanged(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            Debug.Assert(item != null);
            foreach (DataGridColumn column in base.Columns)
            {
                if (column.Header.Equals(item.Header))
                    if (e.RoutedEvent.Equals(MenuItem.CheckedEvent))
                        column.Visibility = Visibility.Visible;
                    else if (e.RoutedEvent.Equals(MenuItem.UncheckedEvent))
                        column.Visibility = Visibility.Collapsed;
            }

            UpdateColumnInfo();
        }

        private void UpdateColumnInfo()
        {
            updatingColumnInfo = true;
            ColumnInformation = SerializeObjectToXML(new ObservableCollection<ColumnInfo>(Columns.Select((x) => new ColumnInfo(x))));
            updatingColumnInfo = false;
        }

        protected override void OnColumnReordered(DataGridColumnEventArgs e)
        {
            UpdateColumnInfo();
            base.OnColumnReordered(e);
        }

        protected override void OnPreviewMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (inWidthChange)
            {
                inWidthChange = false;
                UpdateColumnInfo();
            }
            base.OnPreviewMouseLeftButtonUp(e);
        }

        private void ColumnInfoChanged()
        {
            Items.SortDescriptions.Clear();
            if (string.IsNullOrEmpty(ColumnInformation))
                return;

            var columnInfo = DeserializeFromXml<ObservableCollection<ColumnInfo>>(ColumnInformation);
            foreach (var column in columnInfo)
            {
                var realColumn = Columns.Where((x) => column.Header.Equals(x.Header)).FirstOrDefault();
                if (realColumn == null) { continue; }
                column.Apply(realColumn, Columns.Count, Items.SortDescriptions);
            }
        }

        internal void FillLastColumn()
        {
            DataGridColumn lastColumn = null;
            foreach (DataGridColumn column in base.Columns)
            {
                if (column.Visibility.Equals(Visibility.Visible)
                    && (lastColumn == null || column.DisplayIndex > lastColumn.DisplayIndex))
                    lastColumn = column;
            }
            if (lastColumn != null)
                lastColumn.Width = new DataGridLength(1.0, DataGridLengthUnitType.Star);
        }

        private static string SerializeObjectToXML<T>(T item)
        {
            var xs = new XmlSerializer(typeof(T));
            using var stringWriter = new StringWriter();
            xs.Serialize(stringWriter, item);
            return stringWriter.ToString();
        }

        private static T DeserializeFromXml<T>(string xml)
        {
            T result;
            XmlSerializer ser = new(typeof(T));
            using (TextReader tr = new StringReader(xml))
            {
                result = (T)ser.Deserialize(tr);
            }
            return result;
        }
    }

    public struct ColumnInfo
    {
        public ColumnInfo(DataGridColumn column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));

            Header = column.Header;
            PropertyPath = column is not DataGridComboBoxColumn column1
                ? ((Binding)((DataGridBoundColumn)column).Binding).Path.Path
                : ((Binding)column1.SelectedItemBinding).Path.Path;
            WidthValue = column.Width.DisplayValue;
            WidthType = column.Width.UnitType;
            SortDirection = column.SortDirection;
            DisplayIndex = column.DisplayIndex;
            Visibility = column.Visibility;
        }

        public void Apply(DataGridColumn column, int gridColumnCount, SortDescriptionCollection sortDescriptions)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));
            if (sortDescriptions == null) throw new ArgumentNullException(nameof(sortDescriptions));

            column.Visibility = Visibility;
            column.Width = new DataGridLength(WidthValue, WidthType);
            column.SortDirection = SortDirection;
            if (SortDirection != null)
            {
                sortDescriptions.Add(new SortDescription(PropertyPath, SortDirection.Value));
            }
            if (column.DisplayIndex != DisplayIndex)
            {
                var maxIndex = (gridColumnCount == 0) ? 0 : gridColumnCount - 1;
                column.DisplayIndex = (DisplayIndex <= maxIndex) ? DisplayIndex : maxIndex;
            }
        }

        public object Header;
        public string PropertyPath;
        public ListSortDirection? SortDirection;
        public int DisplayIndex;
        public double WidthValue;
        public DataGridLengthUnitType WidthType;
        public Visibility Visibility;
    }
}

/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
 
namespace WPF_DataGridWithColumnChooser.Controls
{
    public class DataGridExtended : DataGrid
    {
        public DataGridExtended()
            : base()
        {
            theContextMenu = new ContextMenu();
            this.Columns.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Columns_CollectionChanged);
        }
 
        private ContextMenu theContextMenu; //context menu for the field chooser.
        private string AllColumnsHeaders { get; set; }
        private bool oneTime = true;
 
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (oneTime)// do this only once.
            {
                oneTime = false;
                var headersPresenter = FindChild(this);
                // attach the context menu.
                ContextMenuService.SetContextMenu(headersPresenter, theContextMenu);
 
                // update VisibleColumns as necessary.
                if (String.IsNullOrEmpty(VisibleColumns))
                {
                    VisibleColumns = AllColumnsHeaders;
                }
                else
                {
                    string s = VisibleColumns;
                    VisibleColumns = null;
                    VisibleColumns = s;
                }
            }
            return base.ArrangeOverride(finalSize);
        }
 
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            Image icon = (Image)mi.Icon;
            List splited = VisibleColumns.Split(';').ToList();
            string colName = mi.Header.ToString();
 
            // remove empty items.
            for (int i = 0; i  1)
                {
                    splited.Remove(colName);
                }
            }
 
            // update the VisibleColumns.
            string build = "";
            foreach (string name in splited)
            {
                build = string.Format("{0};{1}", name, build);
            }
            VisibleColumns = build;
        }
 
        private void Columns_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
 
            DataGridColumn col = e.NewItems[0] as DataGridColumn;
 
            // keep a list of all clomuns headers for later use.
            AllColumnsHeaders = String.Format("{0};{1}", col.Header.ToString(), AllColumnsHeaders);
 
            // make a new menu item and add it to the context menu.
            MenuItem menuItem = new MenuItem();
            Image img;
            menuItem.Click += new RoutedEventHandler(MenuItem_Click);
            menuItem.Header = col.Header.ToString();
            img = new Image();
            img.Source = Application.Current.Resources["Vmark"] as ImageSource;
            menuItem.Icon = img;
            theContextMenu.Items.Add(menuItem);
 
        }
 
        /// <summary>
        /// Gets or sets a value indicating the names of columns (as they appear in the column header) to be visible, seperated by a semicolon.
        /// columns that whose name is not here will be hidden.
        /// </summary>
        public string VisibleColumns
        {
            get { return (string)GetValue(VisibleColumnsProperty); }
            set { SetValue(VisibleColumnsProperty, value); }
        }
 
        public static readonly DependencyProperty VisibleColumnsProperty =
            DependencyProperty.Register(
                "VisibleColumns",
                typeof(string),
                typeof(DataGridExtended),
                new PropertyMetadata("", new PropertyChangedCallback(OnVisibleColumnsChanged))
                );
 
        private static void OnVisibleColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGridExtended dg = (DataGridExtended)d;
            dg.VisibleColumnsChanged(e);
 
        }
 
        private void VisibleColumnsChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                string[] showTheseColumns = e.NewValue.ToString().Split(';');
                string colName;
 
                // update the columns visibility.
                foreach (DataGridColumn col in this.Columns)
                {
                    colName = col.Header.ToString();
 
                    if (showTheseColumns.Contains(colName))
                        col.Visibility = Visibility.Visible;
                    else
                        col.Visibility = Visibility.Collapsed;
                }
 
                // update the context menu items.
                if (theContextMenu != null)
                {
                    foreach (MenuItem menuItem in theContextMenu.Items)
                    {
                        colName = menuItem.Header.ToString();
                        if (showTheseColumns.Contains(colName))
                            ((Image)menuItem.Icon).Visibility = Visibility.Visible;
                        else
                            ((Image)menuItem.Icon).Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
 
        public static T FindChild(DependencyObject depObj) where T : DependencyObject
        {
            // Confirm obj is valid.
            if (depObj == null) return null;
 
            // success case
            if (depObj is T)
                return depObj as T;
 
            for (int i = 0; i &lt; VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
 
                T obj = FindChild(child);
 
                if (obj != null)
                    return obj;
            }
 
            return null;
        }
    }
 
}
*/
