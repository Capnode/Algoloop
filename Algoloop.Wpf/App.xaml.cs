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
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace Algoloop.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// https://www.icoconverter.com/
    /// </summary>
    public partial class App : Application
    {
        private const uint EsContinous = 0x80000000;
        private const uint EsSystemRequired = 0x00000001;
//        private const uint EsDisplayRequired = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SetThreadExecutionState([In] uint esFlags);

        public App()
        {
            // Workaround for
            // InvalidCastException is thrown when an application with DevExpress controls is used with .NET 6
            DevExpress.Xpf.Core.ClearAutomationEventsHelper.IsEnabled = false;

            // Set working directory to exe directory
            string unc = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(unc);
            Directory.SetCurrentDirectory(folder);

            // Localize WPF string formatting
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set Log handler
            string logfile = Path.Combine(MainService.GetProgramDataFolder(), AboutModel.Product + ".log");
            File.Delete(logfile);
            Log.DebuggingEnabled = Config.GetBool("debug-mode", false);
            Log.DebuggingLevel = Config.GetInt("debug-level", 1);
            Log.LogHandler = new LogItemHandler(logfile);

            // Exception Handling Wiring
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionHandler;

            Algoloop.Wpf.Properties.Settings.Default.Reload();

            // Prevent going to sleep mode
            _ = SetThreadExecutionState(EsContinous | EsSystemRequired);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Enable sleep mode
            _ = SetThreadExecutionState(EsContinous);

            ViewModelLocator.ResearchViewModel.StopJupyter();
            ViewModelLocator.MainViewModel.SaveConfig();
            Wpf.Properties.Settings.Default.Save();

            Log.Trace($"Exit \"{AboutModel.Product}\"");
            base.OnExit(e);
        }

        private void UnobservedTaskExceptionHandler(object sender, UnobservedTaskExceptionEventArgs e)
        {
            var message = nameof(UnobservedTaskExceptionHandler);
            e?.SetObserved(); // Prevents the Program from terminating.

            if (e.Exception != null && e.Exception is Exception tuex)
            {
                message = string.Format(CultureInfo.InvariantCulture, "{0} Exception: {1}", message, tuex.Message);
                Log.Error(tuex, message);
            }
            else if (sender is Exception ex)
            {
                message = string.Format(CultureInfo.InvariantCulture, "{0} Exception: {1}", message, ex.Message);
                Log.Error(ex, message);
            }
        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var message = nameof(UnhandledExceptionHandler);
            if (e.ExceptionObject != null && e.ExceptionObject is Exception uex)
            {
                message = string.Format(CultureInfo.InvariantCulture, "{0} Exception: {1}", message, uex.Message);
                Log.Error(uex, message);
            }
            else if (sender is Exception ex)
            {
                message = string.Format(CultureInfo.InvariantCulture, "{0} Exception: {1}", message, ex.Message);
                Log.Error(ex, message);
            }
        }

        private void DispatcherUnhandledExceptionHandler(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var message = nameof(DispatcherUnhandledExceptionHandler);
            if (e.Exception != null && e.Exception is Exception uex)
            {
                message = string.Format(CultureInfo.InvariantCulture, "{0} Exception: {1}", message, uex.Message);
                Log.Error(uex, message);
            }
            else if (sender is Exception ex)
            {
                message = string.Format(CultureInfo.InvariantCulture, "{0} Exception: {1}", message, ex.Message);
                Log.Error(ex, message);
            }
        }
    }
}
