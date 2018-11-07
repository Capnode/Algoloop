using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;

namespace Algoloop.ViewSupport
{
    static class DataGridExtension
    {
        private static readonly DependencyProperty ColumnBinderProperty = DependencyProperty.RegisterAttached("ColumnBinder", typeof(ColumnBinder), typeof(DataGridExtension));

        public static readonly DependencyProperty ItemPropertiesProperty = DependencyProperty.RegisterAttached(
            "ItemProperties",
            typeof(ObservableCollection<ItemProperty>),
            typeof(DataGridExtension), new PropertyMetadata((d, e) =>
            {
                var dataGrid = d as DataGrid;
                if (dataGrid != null)
                {
                    var columnBinder = dataGrid.GetColumnBinder();
                    if (columnBinder != null)
                        columnBinder.Dispose();

                    var itemProperties = e.NewValue as ObservableCollection<ItemProperty>;
                    if (itemProperties != null)
                    {
                        dataGrid.SetColumnBinder(new ColumnBinder(dataGrid.Dispatcher, dataGrid.Columns, itemProperties));
                    }
                }
            }));

        [AttachedPropertyBrowsableForType(typeof(DataGrid))]
        [DependsOn("ItemsSource")]
        public static ObservableCollection<ItemProperty> GetItemProperties(this DataGrid dataGrid)
        {
            return (ObservableCollection<ItemProperty>)dataGrid.GetValue(ItemPropertiesProperty);
        }

        public static void SetItemProperties(this DataGrid dataGrid, ObservableCollection<ItemProperty> itemProperties)
        {
            dataGrid.SetValue(ItemPropertiesProperty, itemProperties);
        }

        private static ColumnBinder GetColumnBinder(this DataGrid dataGrid)
        {
            return (ColumnBinder)dataGrid.GetValue(ColumnBinderProperty);
        }

        private static void SetColumnBinder(this DataGrid dataGrid, ColumnBinder columnBinder)
        {
            dataGrid.SetValue(ColumnBinderProperty, columnBinder);
        }

        // Takes care of binding ItemProperty collection to DataGridColumn collection.
        // It derives from TypeConverter so it can access SimplePropertyDescriptor class which base class (PropertyDescriptor) is used in DataGrid.GenerateColumns method to inspect if property is read-only.
        // It must be stored in DataGrid (via ColumnBinderProperty attached dependency property) because previous binder must be disposed (CollectionChanged handler must be removed from event), otherwise memory-leak might occur.
        private class ColumnBinder : TypeConverter, IDisposable
        {
            private readonly Dispatcher dispatcher;
            private readonly ObservableCollection<DataGridColumn> columns;
            private readonly ObservableCollection<ItemProperty> itemProperties;

            public ColumnBinder(Dispatcher dispatcher, ObservableCollection<DataGridColumn> columns, ObservableCollection<ItemProperty> itemProperties)
            {
                this.dispatcher = dispatcher;
                this.columns = columns;
                this.itemProperties = itemProperties;

                this.Reset();

                this.itemProperties.CollectionChanged += this.OnItemPropertiesCollectionChanged;
            }

            private void Reset()
            {
                this.columns.Clear();
                foreach (var column in GenerateColumns(itemProperties))
                    this.columns.Add(column);
            }

            private static IEnumerable<DataGridColumn> GenerateColumns(IEnumerable<ItemProperty> itemProperties)
            {
                return DataGrid.GenerateColumns(new ItemProperties(itemProperties));
            }

            private void OnItemPropertiesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                // CollectionChanged is handled in WPF's Dispatcher thread.
                this.dispatcher.Invoke(new Action(() =>
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            int index = e.NewStartingIndex >= 0 ? e.NewStartingIndex : this.columns.Count;
                            foreach (var column in GenerateColumns(e.NewItems.Cast<ItemProperty>()))
                                this.columns.Insert(index++, column);
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            if (e.OldStartingIndex >= 0)
                                for (int i = 0; i < e.OldItems.Count; ++i)
                                    this.columns.RemoveAt(e.OldStartingIndex);
                            else
                                this.Reset();
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            if (e.OldStartingIndex >= 0)
                            {
                                index = e.OldStartingIndex;
                                foreach (var column in GenerateColumns(e.NewItems.Cast<ItemProperty>()))
                                    this.columns[index++] = column;
                            }
                            else
                                this.Reset();
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            this.Reset();
                            break;
                    }
                }));
            }

            public void Dispose()
            {
                this.itemProperties.CollectionChanged -= this.OnItemPropertiesCollectionChanged;
            }

            // Used in DataGrid.GenerateColumns method so that .NET takes care of generating columns from properties.
            private class ItemProperties : IItemProperties
            {
                private readonly ReadOnlyCollection<ItemPropertyInfo> itemProperties;

                public ItemProperties(IEnumerable<ItemProperty> itemProperties)
                {
                    this.itemProperties = new ReadOnlyCollection<ItemPropertyInfo>(itemProperties.Select(itemProperty => new ItemPropertyInfo(itemProperty.Name, itemProperty.PropertyType, new ItemPropertyDescriptor(itemProperty.Name, itemProperty.PropertyType, itemProperty.IsReadOnly))).ToArray());
                }

                ReadOnlyCollection<ItemPropertyInfo> IItemProperties.ItemProperties
                {
                    get { return this.itemProperties; }
                }

                private class ItemPropertyDescriptor : SimplePropertyDescriptor
                {
                    public ItemPropertyDescriptor(string name, Type propertyType, bool isReadOnly)
                        : base(null, name, propertyType, new Attribute[] { isReadOnly ? ReadOnlyAttribute.Yes : ReadOnlyAttribute.No })
                    {
                    }

                    public override object GetValue(object component)
                    {
                        throw new NotSupportedException();
                    }

                    public override void SetValue(object component, object value)
                    {
                        throw new NotSupportedException();
                    }
                }
            }
        }
    }
}