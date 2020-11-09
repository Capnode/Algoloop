/*
 * Copyright 2020 Capnode AB
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

using GalaSoft.MvvmLight;
using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Windows;

namespace Algoloop.Wpf.ViewModel
{
    public abstract class ViewModel : ViewModelBase
    {
        public ViewModel()
        {
            Debug.Assert(IsUiThread());
        }

        public static bool IsUiThread()
        {
//            return Thread.CurrentThread == Application.Current.Dispatcher.Thread;
            return Application.Current.Dispatcher.CheckAccess();
        }

        public static void UiThread(Action action)
        {
            Contract.Requires(action != null);
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
