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

using Algoloop.Wpf.Model;
using Algoloop.Wpf.ViewModels;
using Algoloop.Wpf.Properties;
using QuantConnect.Configuration;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using System.Windows.Interop;
using System.Windows.Input;
using Algoloop.Wpf.Views.Internal;

namespace Algoloop.Wpf.Views
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
            SetTheme(Settings.Default.Theme);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            ScreenExtensions.WINDOWPLACEMENT placement = ScreenExtensions.GetPlacement(hWnd);
            Settings.Default.MainWindowPlacement = placement.ToString();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            try
            {
                ScreenExtensions.WINDOWPLACEMENT placement = new();
                placement.ReadFromBase64String(Settings.Default.MainWindowPlacement);

                double PrimaryMonitorScaling = ScreenExtensions.GetScalingForPoint(new System.Drawing.Point(1, 1));
                double CurrentMonitorScaling = ScreenExtensions.GetScalingForPoint(new System.Drawing.Point(placement.rcNormalPosition.left, placement.rcNormalPosition.top));
                double RescaleFactor = CurrentMonitorScaling / PrimaryMonitorScaling;
                double width = placement.rcNormalPosition.right - placement.rcNormalPosition.left;
                double height = placement.rcNormalPosition.bottom - placement.rcNormalPosition.top;
                placement.rcNormalPosition.right = placement.rcNormalPosition.left + (int)(width / RescaleFactor + 0.5);
                placement.rcNormalPosition.bottom = placement.rcNormalPosition.top + (int)(height / RescaleFactor + 0.5);
                IntPtr hWnd = new WindowInteropHelper(this).Handle;
                ScreenExtensions.SetPlacement(hWnd, placement);
            }
            catch (Exception)
            {
                // Invalid window placement, ignore
            }
        }

        private void FileSettings(object sender, RoutedEventArgs e)
        {
            MainViewModel model = DataContext as MainViewModel;
            model.SaveConfig();
            var settingsView = new SettingsView();
            bool update = settingsView.ShowDialog() ?? false;
            model.DoSettings(update);
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
                Mouse.OverrideCursor = Cursors.Wait;
                SetTheme(item.DataContext.ToString());
                Mouse.OverrideCursor = null;
            }
        }

        private void SetTheme(string actualTheme)
        {
            _themeMenu.Items.Clear();
            foreach (Theme theme in Theme.Themes)
            {
                try
                {
                    var asm = theme.Assembly;
                }
                catch
                {
                    continue;
                }

                if (theme.Name == actualTheme)
                {
                    ApplicationThemeHelper.ApplicationThemeName = theme.Name;
                    Settings.Default.Theme = theme.Name;
                }

                var menuItem = new MenuItem
                {
                    Header = theme.FullName,
                    DataContext = theme.Name,
                    IsChecked = theme.Name == actualTheme
                };

                _themeMenu.Items.Add(menuItem);
            }
        }
    }
}
