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

using Algoloop.Avalonia.Views;
using Algoloop.Wpf.Model;
using Algoloop.Wpf.ViewModels;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Algoloop.Avalonia
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Set working directory to exe directory
            string unc = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(unc) ?? string.Empty;
            Directory.SetCurrentDirectory(folder);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Set Log handler
                string logfile = Path.Combine(MainService.GetProgramDataFolder(), AboutModel.Product + ".log");
                File.Delete(logfile);
                Log.DebuggingEnabled = Config.GetBool("debug-mode", false);
                Log.DebuggingLevel = Config.GetInt("debug-level", 1);
                Log.LogHandler = new LogItemHandler(logfile);

                // Exception Handling Wiring
                AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
                TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionHandler;

                desktop.MainWindow = new MainWindow();
                
                desktop.ShutdownRequested += OnShutdownRequested;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            ViewModelLocator.ResearchViewModel?.StopJupyter();
            ViewModelLocator.MainViewModel?.SaveConfig();
            Log.Trace($"Exit \"{AboutModel.Product}\"");
        }

        public static void LogError(Exception ex, string? message = null)
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
                foreach (Exception exception in rex.LoaderExceptions ?? Array.Empty<Exception>())
                {
                    LogError(exception, message);
                }
            }
        }

        private void UnobservedTaskExceptionHandler(object? sender, UnobservedTaskExceptionEventArgs e)
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
    }
}
