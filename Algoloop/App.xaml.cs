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

using Algoloop.ViewModel;
using System;
using System.Diagnostics;
using System.Windows;

namespace Algoloop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Algoloop.Properties.Settings.Default.Reload();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ViewModelLocator locator = Resources["Locator"] as ViewModelLocator;
            Debug.Assert(locator != null);
            locator.MainViewModel.SaveAll();
            Algoloop.Properties.Settings.Default.Save();
            base.OnExit(e);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs ex)
        {
            Debug.WriteLine("Exception {0} - {1} - {2}", ex.GetType(), ex.Exception.Message, ex.Exception.ToString());
            Exception iex = ex.Exception.InnerException;
            if (iex != null)
                Debug.WriteLine("Exception inner {0} - {1}", iex.GetType(), iex.Message);

            ex.Handled = true; // Continue processing
        }
    }
}
