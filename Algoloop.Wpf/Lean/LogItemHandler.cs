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
using QuantConnect.Logging;
using System;

namespace Algoloop.Lean
{
    /// <summary>
    /// Provides an implementation of <see cref="ILogHandler"/> that writes all log messages to a file on disk.
    /// </summary>
    public class LogItemHandler : ILogItemHandler
    {
        private bool _isDisposed = false; // To detect redundant calls
        private readonly ILogHandler _nlogger = new NLogHandler();
        private Action<LogItem> _logger;

        // we need to control synchronization to our stream writer since it's not inherently thread-safe
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="LogItemHandler"/> class using 'log.txt' for the filepath.
        /// </summary>
        public LogItemHandler()
        {
        }

        public void Connect(Action<LogItem> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="text">The error text to log</param>
        public void Error(string text)
        {
            WriteMessage(LogType.Error, text);
            _nlogger.Error(text);
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The debug text to log</param>
        public void Debug(string text)
        {
            WriteMessage(LogType.Debug, text);
            _nlogger.Debug(text);
        }

        /// <summary>
        /// Write trace message to log
        /// </summary>
        /// <param name="text">The trace text to log</param>
        public void Trace(string text)
        {
            WriteMessage(LogType.Trace, text);
            _nlogger.Trace(text);
        }

        /// <summary>
        /// Writes the message to the log
        /// </summary>
        private void WriteMessage(LogType level, string text)
        {
            lock (_lock)
            {
                if (_isDisposed || _logger == null)
                {
                    return;
                }

                var logItem = new LogItem(DateTime.UtcNow, level, text);
                _logger(logItem);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _nlogger.Dispose();
                 }

                lock (_lock)
                {
                    _isDisposed = true;
                }
            }
        }
    }
}
