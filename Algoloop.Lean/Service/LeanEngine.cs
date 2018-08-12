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

using Algoloop.Lean.Model;
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Algoloop.Lean.Service
{
    public class LeanEngine : MarshalByRefObject
    {
        public (string, string) Run(StrategyJobModel jobModel, AccountModel account)
        {
            if (!SetConfig(jobModel, account))
            {
                return (null, null);
            }

            Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));
            var liveMode = Config.GetBool("live-mode");

            Log.Trace("LeanEngine: Memory " + OS.ApplicationMemoryUsed + "Mb-App  " + +OS.TotalPhysicalMemoryUsed + "Mb-Used  " + OS.TotalPhysicalMemory + "Mb-Total");

            try
            {
                using (var algorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance))
                using (var systemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance))
                {
                    string assemblyPath;
                    systemHandlers.Initialize();
                    var engine = new Engine(systemHandlers, algorithmHandlers, liveMode);
                    var algorithmManager = new AlgorithmManager(liveMode);
                    AlgorithmNodePacket job = systemHandlers.JobQueue.NextJob(out assemblyPath);
                    systemHandlers.LeanManager.Initialize(systemHandlers, algorithmHandlers, job, algorithmManager);
                    engine.Run(job, algorithmManager, assemblyPath);
                    systemHandlers.JobQueue.AcknowledgeJob(job);
                    BacktestResultHandler resultHandler = algorithmHandlers.Results as BacktestResultHandler;
                    return (resultHandler?.JsonResult, resultHandler?.Logs);
                }
            }
            catch (Exception ex)
            {
                Log.LogHandler.Error("{0}: {1}", ex.GetType(), ex.Message);
            }
            finally
            {
                Log.LogHandler.Dispose();
            }

            return (null, null);
        }

        private bool SetConfig(StrategyJobModel jobModel, AccountModel account)
        {
            if (account == null)
            {
                Config.Set("environment", "backtesting");
                Config.Set("live-mode", "false");

                Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
                Config.Set("messaging-handler", "QuantConnect.Messaging.Messaging");
                Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
                Config.Set("api-handler", "QuantConnect.Api.Api");
                Config.Set("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");
                Config.Set("factor-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskFactorFileProvider");
                Config.Set("data-provider", "QuantConnect.Lean.Engine.DataFeeds.DefaultDataProvider");
                Config.Set("alpha-handler", "QuantConnect.Lean.Engine.Alphas.DefaultAlphaHandler");

                Config.Set("setup-handler", "QuantConnect.Lean.Engine.Setup.ConsoleSetupHandler");
                Config.Set("result-handler", "Algoloop.Lean.Service.BacktestResultHandler");
                //            Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.BacktestingResultHandler");
                Config.Set("data-feed-handler", "QuantConnect.Lean.Engine.DataFeeds.FileSystemDataFeed");
                Config.Set("real-time-handler", "QuantConnect.Lean.Engine.RealTime.BacktestingRealTimeHandler");
                Config.Set("history-provider", "QuantConnect.Lean.Engine.HistoricalData.SubscriptionDataReaderHistoryProvider");
                Config.Set("transaction-handler", "QuantConnect.Lean.Engine.TransactionHandlers.BacktestingTransactionHandler");
            }
            else
            {
                Config.Set("environment", "live");
                Config.Set("live-mode", "true");

                Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
                Config.Set("messaging-handler", "QuantConnect.Messaging.Messaging");
                Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
                Config.Set("api-handler", "QuantConnect.Api.Api");
                Config.Set("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");
                Config.Set("factor-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskFactorFileProvider");
                Config.Set("data-provider", "QuantConnect.Lean.Engine.DataFeeds.DefaultDataProvider");
                Config.Set("alpha-handler", "QuantConnect.Lean.Engine.Alphas.DefaultAlphaHandler");

                Config.Set("setup-handler", "QuantConnect.Lean.Engine.Setup.BrokerageSetupHandler");
                Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.LiveTradingResultHandler");
                Config.Set("data-feed-handler", "QuantConnect.Lean.Engine.DataFeeds.LiveTradingDataFeed");
                Config.Set("real-time-handler", "QuantConnect.Lean.Engine.RealTime.LiveTradingRealTimeHandler");
                Config.Set("history-provider", "BrokerageHistoryProvider");
                Config.Set("transaction-handler", "QuantConnect.Lean.Engine.TransactionHandlers.BrokerageTransactionHandler");
                Config.Set("live-mode-brokerage", "FxcmBrokerage");
                Config.Set("data-queue-handler", "FxcmBrokerage");

                Config.Set("fxcm-terminal", Enum.GetName(typeof(AccountModel.AccountType), account.Type));
                Config.Set("fxcm-user-name", account.Login);
                Config.Set("fxcm-password", account.Password);
                Config.Set("fxcm-account-id", account.Id);
            }

            Config.Set("algorithm-language", "CSharp");
            Config.Set("data-folder", "../../../Data/");
            Config.Set("algorithm-location", jobModel.AlgorithmLocation);
            Config.Set("algorithm-type-name", jobModel.AlgorithmName);

            // Set parameters
            var parameters = new Dictionary<string, string>();
            parameters.Add(
                "symbols",
                string.Join(";", jobModel.Symbols.Where(p => p.Enabled).Select(m => m.Name)));
            parameters.Add("resolution", jobModel.Resolution.ToString());
            foreach (ParameterModel parameter in jobModel.Parameters)
            {
                string value;
                if (!parameters.TryGetValue(parameter.Name, out value))
                {
                    parameters.Add(parameter.Name, parameter.Value);
                }
            }

            string parametersConfigString = JsonConvert.SerializeObject(parameters);
            Config.Set("parameters", parametersConfigString);
            return true;
        }
    }
}
