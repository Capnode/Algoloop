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

using QuantConnect.Lean.Engine.Results;
using QuantConnect;
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Logging;
using System;
using QuantConnect.Orders;

namespace Algoloop.ViewModel.Internal.Lean
{
    internal class BacktestResultHandler : BacktestingResultHandler
    {
        public string JsonResult { get; private set; }
        public string Logs { get; private set; }

        /// <summary>
        /// Returns the location of the logs
        /// </summary>
        /// <param name="id">Id that will be incorporated into the algorithm log name</param>
        /// <param name="logs">The logs to save</param>
        /// <returns>The path to the logs</returns>
        public override string SaveLogs(string id, List<LogEntry> logs)
        {
            Logs = string.Join("\r\n", logs);
            return string.Empty;
        }

        /// <summary>
        /// Save the results to Json string
        /// </summary>
        /// <param name="name">The name of the results</param>
        /// <param name="result">The results to save</param>
        public override void SaveResults(string name, Result result)
        {
            JsonResult = JsonConvert.SerializeObject(result);
        }

        protected override void StoreOrderEvents(DateTime utcTime, List<OrderEvent> orderEvents)
        {
        }
    }
}
