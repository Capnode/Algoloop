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

using Algoloop.Model;
using Algoloop.ViewModel;
using Algoloop.Wpf.Properties;
using Algoloop.Wpf.Internal;
using QuantConnect.Configuration;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;

namespace Algoloop.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            string exeFolder = MainService.GetProgramFolder();
            Config.Set("plugin-directory", exeFolder);
            Config.Set("composer-dll-directory", exeFolder);
            SetTheme(ApplicationThemeHelper.ApplicationThemeName);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.MainWindowPlacement = this.GetPlacement();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.SetPlacement(Settings.Default.MainWindowPlacement);
        }

        private async void FileSettings(object sender, RoutedEventArgs e)
        {
            MainViewModel model = DataContext as MainViewModel;
            var settingsView = new SettingsView();
            bool update = settingsView.ShowDialog() ?? false;
            await model.DoSettings(update);
        }

        private void HelpAbout(object sender, RoutedEventArgs e)
        {
            var about = new AboutView();
            about.ShowDialog();
        }

        private void HelpDocumentation(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/Capnode/Algoloop/wiki/Documentation");
        }

        private void HelpTechnicalSupport(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/Capnode/Algoloop/issues");
        }

        private void HelpPrivacyPolicy(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/Capnode/Algoloop/wiki/Privacy-policy");
        }

        private void HelpSubscriptions(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://account.microsoft.com/services");
        }

        private static void OpenUrl(string url)
        {
            try
            {
                url = url.Replace("&", "^&");
                var psi = new ProcessStartInfo("cmd", $"/c start {url}")
                {
                    CreateNoWindow = true
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                // check this: https://github.com/dotnet/corefx/issues/10361
                Debug.WriteLine($"{ex.GetType()}: {ex.Message}");
            }
        }

        private void OnTheme(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem item)
            {
                SetTheme(item.DataContext.ToString());
            }
        }

        private void SetTheme(string actualTheme)
        {
            _themeMenu.Items.Clear();
            foreach (Theme theme in Theme.Themes)
            {
                var menuItem = new MenuItem
                {
                    Header = theme.FullName,
                    DataContext = theme.Name,
                    IsChecked = theme.Name == actualTheme
                };

                _themeMenu.Items.Add(menuItem);
            }

            ApplicationThemeHelper.ApplicationThemeName = actualTheme;
        }
    }
}
