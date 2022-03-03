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
namespace Capnode.Wpf.DataGrid.Internal
{
    internal class ColumnConfiguration : DependencyObject
    {
     
        #region Properties for setting via xaml

        public static readonly DependencyProperty CanUserFreezeProperty =
    DependencyProperty.RegisterAttached("CanUserFreeze", typeof(bool?), typeof(ColumnConfiguration), new PropertyMetadata(null));

        public static void SetCanUserFreeze(DependencyObject element, object o)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            element.SetValue(CanUserFreezeProperty, o);
        }

        public static bool GetCanUserFreeze(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            return (bool)element.GetValue(CanUserFreezeProperty);
        }

        public static readonly DependencyProperty CanUserFilterProperty =
DependencyProperty.RegisterAttached("CanUserFilter", typeof(bool?), typeof(ColumnConfiguration), new PropertyMetadata(null));

        public static void SetCanUserFilter(DependencyObject element, object o)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            element.SetValue(CanUserFilterProperty, o);
        }

        public static bool GetCanUserFilter(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            return (bool)element.GetValue(CanUserFilterProperty);
        }

        public static readonly DependencyProperty CanUserGroupProperty =
DependencyProperty.RegisterAttached("CanUserGroup", typeof(bool?), typeof(ColumnConfiguration), new PropertyMetadata(null));

        public static void SetCanUserGroup(DependencyObject element, object o)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            element.SetValue(CanUserGroupProperty, o);
        }

        public static bool GetCanUserGroup(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            return (bool)element.GetValue(CanUserGroupProperty);
        }

        public static readonly DependencyProperty CanUserSelectDistinctProperty =
DependencyProperty.RegisterAttached("CanUserSelectDistinct", typeof(bool?), typeof(ColumnConfiguration), new PropertyMetadata(null));

        public static void SetCanUserSelectDistinct(DependencyObject element, object o)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            element.SetValue(CanUserSelectDistinctProperty, o);
        }

        public static bool GetCanUserSelectDistinct(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            return (bool)element.GetValue(CanUserSelectDistinctProperty);
        }

        public static readonly DependencyProperty DefaultFilterProperty =
DependencyProperty.RegisterAttached("DefaultFilter", typeof(string), typeof(ColumnConfiguration), new PropertyMetadata(null));

        public static void SetDefaultFilter(DependencyObject element, object o)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            element.SetValue(DefaultFilterProperty, o);
        }

        public static string GetDefaultFilter(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            return (string)element.GetValue(DefaultFilterProperty);
        }

        #endregion
    }
}
