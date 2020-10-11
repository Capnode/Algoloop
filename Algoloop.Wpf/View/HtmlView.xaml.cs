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
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Algoloop.Wpf.View
{
    /// <summary>
    /// Interaction logic for HtmlBox.xaml
    /// </summary>
    public partial class HtmlView : UserControl
    {
        public static readonly DependencyProperty HtmlTextProperty = DependencyProperty.Register("HtmlText", typeof(string), typeof(HtmlView));
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(string), typeof(HtmlView));

        public HtmlView()
        {
            InitializeComponent();
            browser.Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            browser.Navigating += Browser_Navigating;
            browser.SourceUpdated += Browser_SourceUpdated;
        }

        private void Browser_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
        }

        private void Browser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
        }

        public string HtmlText
        {
            get { return (string)GetValue(HtmlTextProperty); }
            set { SetValue(HtmlTextProperty, value); }
        }

        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == HtmlTextProperty)
            {
                if (!string.IsNullOrEmpty(HtmlText))
                {
                    browser.NavigateToString(HtmlText);
                }
            }
            else if (e.Property == SourceProperty)
            {
                if (!string.IsNullOrEmpty(Source))
                {
                    Dispatcher.BeginInvoke((Action)(() => browser.Navigate(Source)));
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WebBrowser wb)
            {
                // Silent
                FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField(
                    "_axIWebBrowser2",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (fiComWebBrowser == null) return;

                object objComWebBrowser = fiComWebBrowser.GetValue(wb);
                if (objComWebBrowser == null) return;

                objComWebBrowser.GetType().InvokeMember(
                    "Silent",
                    BindingFlags.SetProperty,
                    null,
                    objComWebBrowser,
                    new object[] { true },
                    CultureInfo.InvariantCulture);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
