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

using QuantConnect.Logging;
using System;

namespace Algoloop.Wpf.Lean
{
    public class LogItem
    {
        public DateTime Time { get; set; }
        public LogType Level { get; set; }
        public string Message { get; set; }

        public LogItem(DateTime time, LogType level, string message)
        {
            Time = time;
            Level = level;
            Message = message;
        }
    }
}
