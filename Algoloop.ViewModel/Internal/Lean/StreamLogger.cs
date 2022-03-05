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
using System.IO;
using System.Text;

namespace Algoloop.ViewModel.Internal.Lean
{
    internal class StreamLogger : TextWriter
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly ILogHandler _logger;

        public StreamLogger(ILogHandler logger)
        {
            _logger = logger;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char ch)
        {
            base.Write(ch);

            if (ch.Equals('\n'))
            {
                _logger.Trace(_sb.ToString());
                _sb.Clear();
            }
            else if (!ch.Equals('\r'))
            {
                _sb.Append(ch);
            }
        }
    }
}
