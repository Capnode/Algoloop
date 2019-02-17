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
using Algoloop.Service;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace Algoloop.ViewModel
{
    public class LogViewModel : ViewModelBase
    {
        public LogViewModel(ILogHandler logHandler)
        {
            Log.LogHandler = logHandler;

            Log.DebuggingEnabled = Config.GetBool("debug-mode", false);
            Log.DebuggingLevel = Config.GetInt("debug-level", 1);

            ILogItemHandler logService = Log.LogHandler as ILogItemHandler;
            if (logService != null)
            {
                logService.Connect((item) => Logs.Add(item));
            }
        }

        public SyncObservableCollection<LogItem> Logs { get; } = new SyncObservableCollection<LogItem>();
    }
}
