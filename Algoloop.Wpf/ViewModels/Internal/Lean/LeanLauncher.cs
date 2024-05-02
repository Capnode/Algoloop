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
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using static Algoloop.Model.BacktestModel;

namespace Algoloop.Wpf.ViewModels.Internal.Lean
{
    internal class LeanLauncher : IDisposable
    {
        private ConfigProcess _process;
        private bool _isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                _process?.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _process = null;
            _isDisposed = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Run(BacktestModel model, AccountModel account, SettingModel settings, string exeFolder, CancellationToken cancel)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            Debug.Assert(model.Status == CompletionStatus.None);

            bool error = false;
            _process = new ConfigProcess(
                "QuantConnect.Lean.Launcher.exe",
                null,
                Directory.GetCurrentDirectory(),
                true,
                (line) => Log.Trace(line),
                (line) =>
                {
                    error = true;
                    Log.Error(line, true);
                });

            // Set Environment
            StringDictionary environment = _process.Environment;

            // Set config
            IDictionary<string, string> config = _process.Config;
            if (!SetConfig(config, model, account, settings)) return;

            // Start process
            try
            {
                if (model.AlgorithmLanguage.Equals(Language.Python))
                {
                    PythonSupport.SetupPython(_process.Environment, exeFolder);
                }

                model.Active = true;
                _process.Start();
                _process.WaitForExit(cancel, (folder) => PostProcess(folder, model));
                model.Status = error ? CompletionStatus.Error : CompletionStatus.Success;
            }
            catch (OperationCanceledException)
            {
                model.Status = CompletionStatus.Abort;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                model.Status = CompletionStatus.Error;
            }
            finally
            {
                _process.Dispose();
                _process = null;
                model.Active = false;
            }
        }

        private static void PostProcess(string folder, BacktestModel model)
        {
            string resultFile = Path.Combine(folder, $"{model.AlgorithmName}.json");
            if (File.Exists(resultFile))
            {
                model.Result = File.ReadAllText(resultFile);
            }

            string logFile = Path.Combine(folder, $"{model.AlgorithmName}-log.txt");
            if (File.Exists(logFile))
            {
                model.Logs = File.ReadAllText(logFile);
            }
        }

        private static bool SetConfig(
            IDictionary<string, string> config,
            BacktestModel model,
            AccountModel account,
            SettingModel settings)
        {
            SetModel(config, model, settings);
            if (model.Account.Equals(AccountModel.AccountType.Backtest.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                SetBacktest(config);
            }
            else if (model.Account.Equals(AccountModel.AccountType.Paper.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                SetPaper(config);
            }
            else if (account == default)
            {
                Log.Error("No broker selected", true);
                return false;
            }
            return true;
        }

        private static void SetModel(
            IDictionary<string, string> config,
            BacktestModel model,
            SettingModel settings)
        {
            config["debug-mode"] = "false";
            config["debugging"] = "false";
            config["messaging-handler"] = "QuantConnect.Messaging.Messaging";
            config["job-queue-handler"] = "QuantConnect.Queues.JobQueue";
            config["api-handler"] = "QuantConnect.Api.Api";
            config["map-file-provider"] = "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider";
            config["factor-file-provider"] = "QuantConnect.Data.Auxiliary.LocalDiskFactorFileProvider";
//            config["alpha-handler"] = "Algoloop.Lean.AlphaHandler";
            config["alpha-handler"] = "QuantConnect.Lean.Engine.Alphas.DefaultAlphaHandler";
            config["api-access-token"] = settings.ApiToken ?? string.Empty;
            config["job-user-id"] = int.TryParse(settings.ApiUser, out _) ? settings.ApiUser : "0";
            config["job-project-id"] = "0";
            config["algorithm-path-python"] = ".";
            config["regression-update-statistics"] = "false";
            config["algorithm-manager-time-loop-maximum"] = "60";
            config["symbol-minute-limit"] = "10000";
            config["symbol-second-limit"] = "10000";
            config["symbol-tick-limit"] = "10000";
            config["maximum-data-points-per-chart-series"] = "10000";
            config["force-exchange-always-open"] = "false";
            config["version-id"] = "";
            config["security-data-feeds"] = "";
            config["forward-console-messages"] = "true";
            config["send-via-api"] = "false";
            config["lean-manager-type"] = "LocalLeanManager";
            config["transaction-log"] = "";
            config["algorithm-language"] = model.AlgorithmLanguage.ToString();
            config["algorithm-type-name"] = model.AlgorithmName;
            config["algorithm-id"] = model.AlgorithmName;
            config["data-folder"] = settings.DataFolder;
            config["data-directory"] = settings.DataFolder;
            config["cache-location"] = settings.DataFolder;
            string fullPath = MainService.FullExePath(model.AlgorithmLocation);
            config["algorithm-location"] = fullPath;
            string fullFolder = MainService.GetProgramFolder();
            config["plugin-directory"] = fullFolder;
            config["composer-dll-directory"] = fullFolder;
            config["results-destination-folder"] = ".";
            config["log-handler"] = "CompositeLogHandler";
            config["scheduled-event-leaky-bucket-capacity"] = "120";
            config["scheduled-event-leaky-bucket-time-interval-minutes"] = "1440";
            config["scheduled-event-leaky-bucket-refill-amount"] = "18";
            config["object-store"] = "LocalObjectStore";
            config["object-store-root"] = "./storage";
            config["data-permission-manager"] = "DataPermissionManager";
            config["results-destination-folder"] = ".";
            config["ignore-version-checks"] = "false";
            config["data-feed-workers-count"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture);
            config["data-feed-max-work-weight"] = "400";
            config["data-feed-queue-type"] = "QuantConnect.Lean.Engine.DataFeeds.WorkScheduling.WorkQueue, QuantConnect.Lean.Engine";
            config["show-missing-data-logs"] = "false";
            config["close-automatically"] = "true";
            config["live-data-url"] = "ws://www.quantconnect.com/api/v2/live/data/";
            config["live-data-port"] = "8020";
            if (settings.ApiDownload)
            {
                config["data-provider"] = "QuantConnect.Lean.Engine.DataFeeds.ApiDataProvider";
            }
            else
            {
                config["data-provider"] = "QuantConnect.Lean.Engine.DataFeeds.DefaultDataProvider";
            }

            if (model.IsDataValid)
            {
                config["period-start"] = model.StartDate.ToString(CultureInfo.InvariantCulture);
                config["period-finish"] = model.EndDate.ToString(CultureInfo.InvariantCulture);
                config["cash-amount"] = model.InitialCapital.ToString(CultureInfo.InvariantCulture);
                SetParameters(config, model);
            }
        }

        private static void SetParameters(
            IDictionary<string, string> config,
            BacktestModel model)
        {
            var parameters = new Dictionary<string, string>
            {
                { "security", model.Security.ToStringInvariant() },
                { "resolution", model.Resolution.ToStringInvariant() }
            };

            if (!string.IsNullOrEmpty(model.Market))
            {
                parameters.Add("market", model.Market);
            }

            if (!model.Symbols.IsNullOrEmpty())
            {
                parameters.Add("symbols", string.Join(";", model.Symbols.Where(p => p.Active).Select(m => m.Id)));
            }

            foreach (ParameterModel parameter in model.Parameters)
            {
                if (!parameters.TryGetValue(parameter.Name, out string value))
                {
                    if (parameter.UseValue)
                    {
                        parameters.Add(parameter.Name, parameter.Value);
                    }
                }
            }

            string parametersConfigString = JsonConvert.SerializeObject(parameters);
            config["parameters"] = parametersConfigString;
        }

        private static void SetBacktest(IDictionary<string, string> config)
        {
            config["environment"] = "backtesting";
            config["live-mode"] = "false";
            config["setup-handler"] = "QuantConnect.Lean.Engine.Setup.BacktestingSetupHandler";
//                config["setup-handler"] = "QuantConnect.Lean.Engine.Setup.ConsoleSetupHandler";
//            config["result-handler"] = "Algoloop.Lean.BacktestResultHandler";
            config["result-handler"] = "QuantConnect.Lean.Engine.Results.BacktestingResultHandler";
            config["data-feed-handler"] = "QuantConnect.Lean.Engine.DataFeeds.FileSystemDataFeed";
            config["real-time-handler"] = "QuantConnect.Lean.Engine.RealTime.BacktestingRealTimeHandler";
            config["history-provider"] = "QuantConnect.Lean.Engine.HistoricalData.SubscriptionDataReaderHistoryProvider";
            config["transaction-handler"] = "QuantConnect.Lean.Engine.TransactionHandlers.BacktestingTransactionHandler";
        }

        private static void SetPaper(IDictionary<string, string> config)
        {
            config["environment"] = "live-paper";
            config["live-mode"] = "true";
            config["live-mode-brokerage"] = "PaperBrokerage";
            config["setup-handler"] = "QuantConnect.Lean.Engine.Setup.BrokerageSetupHandler";
            config["result-handler"] = "QuantConnect.Lean.Engine.Results.LiveTradingResultHandler";
            config["data-feed-handler"] = "QuantConnect.Lean.Engine.DataFeeds.LiveTradingDataFeed";
            config["data-queue-handler"] = "QuantConnect.Lean.Engine.DataFeeds.Queues.LiveDataQueue";
            config["real-time-handler"] = "QuantConnect.Lean.Engine.RealTime.LiveTradingRealTimeHandler";
            config["transaction-handler"] = "QuantConnect.Lean.Engine.TransactionHandlers.BacktestingTransactionHandler";
        }
    }
}
