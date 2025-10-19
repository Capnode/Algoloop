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
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
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
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Set working directory to exe directory
                string? unc = Assembly.GetExecutingAssembly().Location;
                string? folder = Path.GetDirectoryName(unc);
                if (folder != null)
                {
                    Directory.SetCurrentDirectory(folder);
                }

                // Exception Handling Wiring
                AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
                TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionHandler;

                desktop.MainWindow = new MainWindow();
                
                desktop.Exit += (s, e) =>
                {
                    // Cleanup logic will go here when integrated with full ViewModels
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        public static void LogError(Exception ex, string? message = null)
        {
            Console.Error.WriteLine(message != null 
                ? $"{message} {ex.GetType()}: {ex.Message}" 
                : $"{ex.GetType()}: {ex.Message}");

            if (ex.InnerException != null)
            {
                LogError(ex.InnerException, message);
            }

            if (ex is ReflectionTypeLoadException rex)
            {
                foreach (Exception? exception in rex.LoaderExceptions)
                {
                    if (exception != null)
                    {
                        LogError(exception, message);
                    }
                }
            }
        }

        private void UnobservedTaskExceptionHandler(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            e?.SetObserved(); // Prevents the Program from terminating.

            if (e.Exception != null)
            {
                LogError(e.Exception, nameof(UnobservedTaskExceptionHandler));
            }
            else if (sender is Exception ex)
            {
                LogError(ex, nameof(UnobservedTaskExceptionHandler));
            }
        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception uex)
            {
                LogError(uex, nameof(UnhandledExceptionHandler));
            }
            else if (sender is Exception ex)
            {
                LogError(ex, nameof(UnhandledExceptionHandler));
            }
        }
    }
}
