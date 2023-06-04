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

using Algoloop.ViewModel;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Algoloop.Wpf
{
    /// <summary>
    /// Interaction logic for MarketsView.xaml
    /// </summary>
    public partial class MarketsView : UserControl
    {
        private readonly Dictionary<TextBlock, double> _cellValue = new();
        private readonly Storyboard _greenBlink = new();
        private readonly Storyboard _redBlink = new();
        private bool _isMouseDown;

        public MarketsView()
        {
            InitializeComponent();

            System.Drawing.Color green = System.Drawing.Color.LightGreen;
            ColorAnimation animation = new()
            {
                BeginTime = new TimeSpan(0, 0, 0, 0, 200),
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, 500)),
                From = Color.FromArgb(green.A, green.R, green.G, green.B),
                To = Color.FromArgb(0, 0, 0, 0)
            };
            Storyboard.SetTargetProperty(animation, new PropertyPath("(TextBlock.Background).(SolidColorBrush.Color)"));
            _greenBlink.Children.Add(animation);

            System.Drawing.Color red = System.Drawing.Color.Salmon;
            animation = new ColorAnimation
            {
                BeginTime = new TimeSpan(0, 0, 0, 0, 200),
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, 500)),
                From = Color.FromArgb(red.A, red.R, red.G, red.B),
                To = Color.FromArgb(0, 0, 0, 0)
            };
            Storyboard.SetTargetProperty(animation, new PropertyPath("(TextBlock.Background).(SolidColorBrush.Color)"));
            _redBlink.Children.Add(animation);
        }

        private void TargetUpdatedBlinkCell(object sender, DataTransferEventArgs e)
        {
            if (e.TargetObject is not TextBlock tb)
                return;

            if (!_cellValue.ContainsKey(tb))
            {
                _cellValue[tb] = 0;
                tb.Background = Brushes.Transparent;
                tb.Foreground = Brushes.Black;
            }

            double prevValue = _cellValue[tb];
            string text = tb.Text.Replace((char)8722, '-'); // Convert Unicode minus
            if (!double.TryParse(text, out double currValue))
                return;

            if (prevValue < currValue)
            {
                Storyboard.SetTarget(_greenBlink, tb);
                _greenBlink.Begin();
            }
            else if (prevValue > currValue)
            {
                Storyboard.SetTarget(_redBlink, tb);
                _redBlink.Begin();
            }

            _cellValue[tb] = currValue;
        }

        private void TreeView_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _isMouseDown = false;
            if (sender is TreeView tree
                && FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource) is TreeViewItem treeViewItem
                && treeViewItem.DataContext is SymbolViewModel
                && e.LeftButton.Equals(MouseButtonState.Pressed)
                && tree.SelectedItem != null)
            {
                DragDrop.DoDragDrop(treeViewItem, tree.SelectedItem, DragDropEffects.Move);
            }
        }

        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(SymbolViewModel)))
            {
                if (FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource) is TreeViewItem treeViewItem)
                {
                    if (treeViewItem.DataContext is SymbolViewModel)
                    {
                        treeViewItem.IsSelected = true;
                        e.Effects = DragDropEffects.Move;
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.Move;
                    e.Handled = true;
                    return;
                }
            }
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            if (sender is TreeView && e.Data.GetDataPresent(typeof(SymbolViewModel)))
            {
                TreeViewItem treeViewItem = FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource);
                object dropTarget = treeViewItem == null ? DataContext : treeViewItem.Header;
                if (dropTarget == null || e.Data.GetData(typeof(SymbolViewModel)) is not SymbolViewModel dragSource || dropTarget == dragSource) return;
                //if (dragSource.MoveStrategyCommand.CanExecute(dropTarget))
                //{
                //    dragSource.MoveStrategyCommand.Execute(dropTarget);
                //}
            }
        }

        private void Reference_Drop(object sender, DragEventArgs e)
        {
            if (sender is not ComboBox && e.Data.GetDataPresent(typeof(SymbolViewModel))) return;
            if (DataContext is not MarketsViewModel markets) return;
            if (markets.SelectedItem is not SymbolViewModel symbol) return;
            if (e.Data.GetData(typeof(SymbolViewModel)) is not SymbolViewModel reference) return;
            symbol.AddReferenceSymbol(reference);
        }

        private static T FindAnchestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T t)
                {
                    return t;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            _isMouseDown = true;
        }

        private void TreeView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            _isMouseDown = false;
            if (sender is not TreeView treeView) return;
            if (DataContext is not MarketsViewModel viewModel) return;
            if (!viewModel.SelectedChangedCommand.CanExecute(treeView.SelectedItem)) return;
            viewModel.SelectedChangedCommand.Execute(treeView.SelectedItem);

            //var item = ((DependencyObject)e.OriginalSource);
            //while (item != null && !(item is TreeViewItem))
            //    item = VisualTreeHelper.GetParent(item);
            //var tvi = item as TreeViewItem;
            //if (tvi != null)
            //{
            //    tvi.IsSelected = true;
            //}
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_isMouseDown) return;
            if (sender is not TreeView treeView) return;
            if (DataContext is not MarketsViewModel viewModel) return;
            if (!viewModel.SelectedChangedCommand.CanExecute(treeView.SelectedItem)) return;
            viewModel.SelectedChangedCommand.Execute(treeView.SelectedItem);
        }
    }
}
