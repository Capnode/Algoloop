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
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Algoloop.Service
{
    public class LeanEngine : MarshalByRefObject
    {
        public StrategyJobModel Run(StrategyJobModel model, AccountModel account, HostDomainLogger logger)
        {
            Log.LogHandler = logger;
            SetConfig(model, account);

            var liveMode = Config.GetBool("live-mode");
            Log.Trace("LeanEngine: Memory " + OS.ApplicationMemoryUsed + "Mb-App " + OS.TotalPhysicalMemoryUsed + "Mb-Used");

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
                    model.Result = resultHandler?.JsonResult;
                    model.Logs = resultHandler?.Logs;
                }
            }
            catch (Exception ex)
            {
                Log.Error("{0}: {1}", ex.GetType(), ex.Message);
            }

            Log.LogHandler.Dispose();
            return model;
        }

        private void SetConfig(StrategyJobModel model, AccountModel account)
        {
            Config.Set("messaging-handler", "QuantConnect.Messaging.Messaging");
            Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
            Config.Set("api-handler", "QuantConnect.Api.Api");
            Config.Set("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");
            Config.Set("factor-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskFactorFileProvider");
            Config.Set("data-provider", "QuantConnect.Lean.Engine.DataFeeds.DefaultDataProvider");
            Config.Set("alpha-handler", "QuantConnect.Lean.Engine.Alphas.DefaultAlphaHandler");

            if (account == null)
            {
                Config.Set("environment", "backtesting");
                Config.Set("live-mode", "false");

                Config.Set("setup-handler", "QuantConnect.Lean.Engine.Setup.BacktestingSetupHandler");
//                Config.Set("setup-handler", "QuantConnect.Lean.Engine.Setup.ConsoleSetupHandler");
                Config.Set("result-handler", "Algoloop.Service.BacktestResultHandler");
                //            Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.BacktestingResultHandler");
                Config.Set("data-feed-handler", "QuantConnect.Lean.Engine.DataFeeds.FileSystemDataFeed");
                Config.Set("real-time-handler", "QuantConnect.Lean.Engine.RealTime.BacktestingRealTimeHandler");
                Config.Set("history-provider", "QuantConnect.Lean.Engine.HistoricalData.SubscriptionDataReaderHistoryProvider");
                Config.Set("transaction-handler", "QuantConnect.Lean.Engine.TransactionHandlers.BacktestingTransactionHandler");
            }
            else if (account.Account.Equals(AccountModel.AccountType.Paper))
            {
                Config.Set("live-mode", "true");
                Config.Set("live-mode-brokerage", "PaperBrokerage");
                Config.Set("setup-handler", "QuantConnect.Lean.Engine.Setup.BrokerageSetupHandler");
                Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.LiveTradingResultHandler");
                Config.Set("data-feed-handler", "QuantConnect.Lean.Engine.DataFeeds.LiveTradingDataFeed");
                Config.Set("data-queue-handler", "QuantConnect.Lean.Engine.DataFeeds.Queues.LiveDataQueue");
                Config.Set("real-time-handler", "QuantConnect.Lean.Engine.RealTime.LiveTradingRealTimeHandler");
                Config.Set("transaction-handler", "QuantConnect.Lean.Engine.TransactionHandlers.BacktestingTransactionHandler");
            }
            else if (account.Account.Equals(AccountModel.AccountType.Fxcm))
            {
                Config.Set("environment", "live");
                Config.Set("live-mode", "true");

                Config.Set("setup-handler", "QuantConnect.Lean.Engine.Setup.BrokerageSetupHandler");
                Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.LiveTradingResultHandler");
                Config.Set("data-feed-handler", "QuantConnect.Lean.Engine.DataFeeds.LiveTradingDataFeed");
                Config.Set("real-time-handler", "QuantConnect.Lean.Engine.RealTime.LiveTradingRealTimeHandler");
                Config.Set("history-provider", "BrokerageHistoryProvider");
                Config.Set("transaction-handler", "QuantConnect.Lean.Engine.TransactionHandlers.BrokerageTransactionHandler");
                Config.Set("live-mode-brokerage", "FxcmBrokerage");
                Config.Set("data-queue-handler", "FxcmBrokerage");

                Config.Set("force-exchange-always-open", "false");
                switch (account.Access)
                {
                    case AccountModel.AccessType.Demo:
                        Config.Set("fxcm-terminal", "Demo");
                        break;

                    case AccountModel.AccessType.Real:
                        Config.Set("fxcm-terminal", "Real");
                        break;
                }

                Config.Set("fxcm-user-name", account.Login);
                Config.Set("fxcm-password", account.Password);
                Config.Set("fxcm-account-id", account.Id);
            }

            Config.Set("api-access-token", "");
            Config.Set("job-user-id", "0");
            Config.Set("job-project-id", "0");
            Config.Set("algorithm-path-python", "../../../Algorithm.Python/");
            Config.Set("regression-update-statistics", "false");
            Config.Set("algorithm-manager-time-loop-maximum", "20");
            Config.Set("symbol-minute-limit", "10000");
            Config.Set("symbol-second-limit", "10000");
            Config.Set("symbol-tick-limit", "10000");
            Config.Set("maximum-data-points-per-chart-series", "4000");
            Config.Set("version-id", "");
            Config.Set("security-data-feeds", "");
            Config.Set("forward-console-messages", "true");
            Config.Set("send-via-api", "false");
            Config.Set("lean-manager-type", "LocalLeanManager");
            Config.Set("transaction-log", "");

            Config.Set("algorithm-language", "CSharp");
            Config.Set("data-folder", model.DataFolder);
            Config.Set("data-directory", model.DataFolder);
            Config.Set("cache-location", model.DataFolder);
            string fullPath = Path.GetFullPath(model.AlgorithmLocation);
            Config.Set("algorithm-location", fullPath);
            string fullFolder = Path.GetDirectoryName(fullPath);
            Config.Set("plugin-directory", fullFolder);
            Config.Set("composer-dll-directory", fullFolder);
            Config.Set("algorithm-type-name", model.AlgorithmName);

            // Set parameters
            var parameters = new Dictionary<string, string>();
            parameters.Add("startdate", model.StartDate.ToString());
            parameters.Add("enddate", model.EndDate.ToString());
            parameters.Add("cash", model.InitialCapital.ToString());
            parameters.Add("resolution", model.Resolution.ToString());
            parameters.Add("market", model.Provider.ToString());
            parameters.Add(
                "symbols",
                string.Join(";", model.Symbols.Where(p => p.Active).Select(m => m.Name)));

            foreach (ParameterModel parameter in model.Parameters)
            {
                string value;
                if (!parameters.TryGetValue(parameter.Name, out value))
                {
                    if (parameter.UseValue)
                    {
                        parameters.Add(parameter.Name, parameter.Value);
                    }
                }
            }

            string parametersConfigString = JsonConvert.SerializeObject(parameters);
            Config.Set("parameters", parametersConfigString);
        }
    }
}
