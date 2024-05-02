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

namespace Algoloop.Wpf.ViewModels.Internal.Lean
{
    internal class HostDomainLogger : MarshalByRefObject, ILogHandler
    {
        private bool _isDisposed = false; // To detect redundant calls
        private int _errCount = 0;

        public bool IsError => _errCount > 0;

        public void Debug(string text)
        {
            Log.Debug(text);
        }

        public void Error(string text)
        {
            _errCount++;
            Log.Error(text, true);
        }

        public void Trace(string text)
        {
            Log.Trace(text, true);
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
                }

                _isDisposed = true;
            }
        }
    }
}
