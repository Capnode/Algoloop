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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Algoloop.Wpf
{
    /// <summary>
    /// Interaction logic for StrategiesView.xaml
    /// </summary>
    public partial class StrategiesView : UserControl
    {
        public StrategiesView()
        {
            InitializeComponent();
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is TreeView tree
                && FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource) is TreeViewItem treeViewItem
                && treeViewItem.DataContext is StrategyViewModel
                && e.LeftButton.Equals(MouseButtonState.Pressed)
                && tree.SelectedItem != null)
            {
                DragDrop.DoDragDrop(treeViewItem, tree.SelectedItem, DragDropEffects.Move);
            }
        }

        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(StrategyViewModel)))
            {
                if (FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource) is TreeViewItem treeViewItem)
                {
                    if (treeViewItem.DataContext is StrategyViewModel)
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
            if (sender is TreeView && e.Data.GetDataPresent(typeof(StrategyViewModel)))
            {
                TreeViewItem treeViewItem = FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource);
                object dropTarget = treeViewItem == null ? DataContext : treeViewItem.Header;
                if (dropTarget == null || e.Data.GetData(typeof(StrategyViewModel)) is not StrategyViewModel dragSource || dropTarget == dragSource) return;
                if (dragSource.MoveStrategyCommand.CanExecute(dropTarget))
                {
                    dragSource.MoveStrategyCommand.Execute(dropTarget);
                }
            }
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
    }
}
