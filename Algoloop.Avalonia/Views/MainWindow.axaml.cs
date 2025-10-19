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
using Avalonia.Controls;
using Avalonia.Interactivity;
using QuantConnect.Configuration;
using System;
using System.Diagnostics;

namespace Algoloop.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            string exeFolder = MainService.GetProgramFolder();
            Config.Set("plugin-directory", exeFolder);
            Config.Set("composer-dll-directory", exeFolder);
        }

        private void OnFileSettings(object? sender, RoutedEventArgs e)
        {
            // Settings dialog - to be implemented
            if (DataContext is MainViewModel model)
            {
                model.SaveConfig();
                // TODO: Show settings dialog
                model.DoSettings(false);
            }
        }

        private void OnHelpAbout(object? sender, RoutedEventArgs e)
        {
            // About dialog - to be implemented
            // TODO: Show about dialog
        }

        private void OnHelpDocumentation(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/Capnode/Algoloop/wiki/Documentation");
        }

        private void OnHelpTechnicalSupport(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/Capnode/Algoloop/issues");
        }

        private void OnHelpPrivacyPolicy(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/Capnode/Algoloop/wiki/Privacy-policy");
        }

        private static void OpenUrl(string url)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    url = url.Replace("&", "^&");
                    var psi = new ProcessStartInfo("cmd", $"/c start {url}")
                    {
                        CreateNoWindow = true
                    };
                    Process.Start(psi);
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", url);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", url);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.GetType()}: {ex.Message}");
            }
        }
    }
}
