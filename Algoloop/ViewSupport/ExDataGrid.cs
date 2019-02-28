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

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Algoloop.ViewSupport
{
    public class ExDataGrid : DataGrid
    {
        public static readonly DependencyProperty ExColumnsProperty = DependencyProperty.Register("ExColumns",
            typeof(ObservableCollection<DataGridColumn>), typeof(ExDataGrid),
                new PropertyMetadata(OnDataGridColumnsPropertyChanged));

        public ObservableCollection<DataGridColumn> ExColumns
        {
            get { return (ObservableCollection<DataGridColumn>)base.GetValue(ExColumnsProperty); }
            set { base.SetValue(ExColumnsProperty, value); }
        }

        private static void OnDataGridColumnsPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var context = source as ExDataGrid;

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
    }
}