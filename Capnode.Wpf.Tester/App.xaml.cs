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

using Capnode.Wpf.Tester.Properties;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace Capnode.Wpf.Tester
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string Logger = "Application.Log";

        public App()
        {
            File.WriteAllText(Logger, "Application start\n");

            // Localize WPF string formatting
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Exception Handling Wiring
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            Tester.Properties.Settings.Default.Reload();
            File.AppendAllText(Logger, $"ExColumnInfo:{Settings.Default.ExColumnsInfo}\n");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            File.AppendAllText(Logger, $"ExColumnInfo:{Settings.Default.ExColumnsInfo}\n");
            File.AppendAllText(Logger, "Application stop\n");
            Settings.Default.Save();

            base.OnExit(e);
        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var message = nameof(UnhandledExceptionHandler);
            if (e.ExceptionObject != null && e.ExceptionObject is Exception uex)
            {
                message = string.Format(CultureInfo.InvariantCulture, "{0} Exception: {1}\n", message, uex.Message);
                File.AppendAllText(Logger, message);
            }
            else if (sender is Exception ex)
            {
                message = string.Format(CultureInfo.InvariantCulture, "{0} Exception: {1}\n", message, ex.Message);
                File.AppendAllText(Logger, message);
            }
        }
    }
}
