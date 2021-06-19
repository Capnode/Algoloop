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
using Algoloop.Support;
using Algoloop.Wpf.Lean;
using Algoloop.Wpf.ViewModel;
using CefSharp;
using CefSharp.Wpf;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace Algoloop.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const uint _esContinous = 0x80000000;
        private const uint _esSystemRequired = 0x00000001;
//        private const uint _esDisplayRequired = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SetThreadExecutionState([In] uint esFlags);

        public App()
        {
            InitCef();

            // Set working directory to exe directory
            string unc = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(unc);
            Directory.SetCurrentDirectory(folder);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set Log handler
            string logfile = Path.Combine(MainService.GetAppDataFolder(), AboutModel.AssemblyProduct + ".log");
            File.Delete(logfile);
            Log.DebuggingEnabled = Config.GetBool("debug-mode", false);
            Log.DebuggingLevel = Config.GetInt("debug-level", 1);
            Log.LogHandler = new LogItemHandler(logfile);
            Log.Trace($">Startup \"{AboutModel.AssemblyProduct}\"");
            Log.Trace($"ProgramFolder={MainService.GetProgramFolder()}");
            Log.Trace($"AppDataFolder={MainService.GetAppDataFolder()}");
            Log.Trace($"ProgramDataFolder={MainService.GetProgramDataFolder()}");
            Log.Trace($"UserDataFolder={MainService.GetUserDataFolder()}");

            // Exception Handling Wiring
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionHandler;

            Algoloop.Wpf.Properties.Settings.Default.Reload();

            // Prevent going to sleep mode
            _ = SetThreadExecutionState(_esContinous | _esSystemRequired);
            Log.Trace($"<OnStartup");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Enable sleep mode
            _ = SetThreadExecutionState(_esContinous);

            ViewModelLocator.ResearchViewModel.StopJupyter();
            ViewModelLocator.MainViewModel.SaveConfig();
            Algoloop.Wpf.Properties.Settings.Default.Save();

            Log.Trace($"Exit \"{AboutModel.AssemblyProduct}\"");
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

        private void InitCef()
        {
            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            };

            //Example of setting a command line argument
            //Enables WebRTC
            // - CEF Doesn't currently support permissions on a per browser basis see https://bitbucket.org/chromiumembedded/cef/issues/2582/allow-run-time-handling-of-media-access
            // - CEF Doesn't currently support displaying a UI for media access permissions
            //
            //NOTE: WebRTC Device Id's aren't persisted as they are in Chrome see https://bitbucket.org/chromiumembedded/cef/issues/2064/persist-webrtc-deviceids-across-restart
            settings.CefCommandLineArgs.Add("enable-media-stream");
            //https://peter.sh/experiments/chromium-command-line-switches/#use-fake-ui-for-media-stream
            settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
            //For screen sharing add (see https://bitbucket.org/chromiumembedded/cef/issues/2582/allow-run-time-handling-of-media-access#comment-58677180)
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");

            //Example of checking if a call to Cef.Initialize has already been made, we require this for
            //our .Net 5.0 Single File Publish example, you don't typically need to perform this check
            //if you call Cef.Initialze within your WPF App constructor.
            if (!Cef.IsInitialized)
            {
                //Perform dependency check to make sure all relevant resources are in our output directory.
                Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            }
        }
    }
}
