/*
 * Copyright 2021 Capnode AB
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

using System;
using System.Windows;

namespace Algoloop.Wpf.ViewSupport
{
    public class WpfSupport
    {
        public static bool IsUiThread()
        {
            Application app = Application.Current;

            // Application do not exist in test environment
            if (app == null) return true;
            return app.Dispatcher.CheckAccess();
            //            return Thread.CurrentThread == Application.Current.Dispatcher.Thread;
        }

        public static void UiThread(Action action)
        {
            if (IsUiThread())
            {
                // Run in UI thread
                action();
            }
            else
            {
                // Dispatch to UI thread
                Application.Current.Dispatcher.Invoke(action);
            }
        }
    }
}
