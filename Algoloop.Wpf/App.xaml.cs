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


        public static void LogError(Exception ex, string message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                Log.Error($"{ex.GetType()}: {ex.Message}", true);
            }
            else
            {
                Log.Error($"{message} {ex.GetType()}: {ex.Message}", true);
            }

            if (ex.InnerException != null)
            {
                LogError(ex.InnerException, message);
            }

            if (ex is ReflectionTypeLoadException rex)
            {
                foreach (Exception exception in rex.LoaderExceptions)
                {
                    LogError(exception, message);
                }
            }
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

            Wpf.Properties.Settings.Default.Reload();

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
            e?.SetObserved(); // Prevents the Program from terminating.

            if (e.Exception != null && e.Exception is Exception tuex)
            {
                LogError(tuex, nameof(UnobservedTaskExceptionHandler));
            }
            else if (sender is Exception ex)
            {
                LogError(ex, nameof(UnobservedTaskExceptionHandler));
            }
        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject != null && e.ExceptionObject is Exception uex)
            {
                LogError(uex, nameof(UnhandledExceptionHandler));
            }
            else if (sender is Exception ex)
            {
                Log.Error(ex, nameof(UnhandledExceptionHandler));
            }
        }

        private void DispatcherUnhandledExceptionHandler(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception != null && e.Exception is Exception uex)
            {
                LogError(uex, nameof(DispatcherUnhandledExceptionHandler));
            }
            else if (sender is Exception ex)
            {
                LogError(ex, nameof(DispatcherUnhandledExceptionHandler));
            }
        }
    }
}
