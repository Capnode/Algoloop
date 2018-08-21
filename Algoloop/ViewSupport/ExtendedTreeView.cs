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

 using System.Windows;
using System.Windows.Controls;

namespace Algoloop.ViewSupport
{
	public class ExtendedTreeView : TreeView
	{
		public ExtendedTreeView() : base()
		{
			SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(TreeViewEx_SelectedItemChanged);
		}

		private void TreeViewEx_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			SelectedItem = e.NewValue;
		}

		/// <summary>
		/// Gets or Sets the SelectedItem possible Value of the TreeViewItem object.
		/// </summary>
		public new object SelectedItem
		{
			get { return this.GetValue(SelectedItemProperty); }
			set { this.SetValue(SelectedItemProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public new static readonly DependencyProperty SelectedItemProperty =
				DependencyProperty.Register("SelectedItem", typeof(object), typeof(ExtendedTreeView),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedItemProperty_Changed));

		static void SelectedItemProperty_Changed(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			ExtendedTreeView targetObject = dependencyObject as ExtendedTreeView;
			if (targetObject != null)
			{
				TreeViewItem tvi = targetObject.FindItemNode(targetObject.SelectedItem) as TreeViewItem;
				if (tvi != null)
					tvi.IsSelected = true;
			}
		}

		public TreeViewItem FindItemNode(object item)
		{
			TreeViewItem node = null;
			foreach (object data in this.Items)
			{
				node = this.ItemContainerGenerator.ContainerFromItem(data) as TreeViewItem;
				if (node != null)
				{
					if (data == item)
						break;
					node = FindItemNodeInChildren(node, item);
					if (node != null)
						break;
				}
			}
			return node;
		}

		protected TreeViewItem FindItemNodeInChildren(TreeViewItem parent, object item)
		{
			TreeViewItem node = null;
			bool isExpanded = parent.IsExpanded;
			if (!isExpanded) //Can't find child container unless the parent node is Expanded once
			{
				parent.IsExpanded = true;
				parent.UpdateLayout();
			}
			foreach (object data in parent.Items)
			{
				node = parent.ItemContainerGenerator.ContainerFromItem(data) as TreeViewItem;
				if (data == item && node != null)
					break;
				node = FindItemNodeInChildren(node, item);
				if (node != null)
					break;
			}
			if (node == null && parent.IsExpanded != isExpanded)
				parent.IsExpanded = isExpanded;
			if (node != null)
				parent.IsExpanded = true;
			return node;
		}
	}
}