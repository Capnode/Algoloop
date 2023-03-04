/*
 * Copyright 2019 Capnode AB
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Alphas;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static QuantConnect.Tests.AlgorithmRunner;

namespace Algoloop.Algorithm.CSharp.Algo.Tests
{
    public static class TestEngine
    {
        public static Dictionary<string, string> Run(
            string algorithm,
            string algorithmLocation,
            DateTime startDate,
            DateTime endDate,
            decimal? initialCash,
            Dictionary<string, string> parameters)
        {
            string setupHandler = "RegressionSetupHandlerWrapper";
            AlgorithmStatus expectedFinalStatus = AlgorithmStatus.Completed;
            Dictionary<string, string> expectedStatistics = new ();
            AlphaRuntimeStatistics expectedAlphaStatistics = null;
            Language language = Language.CSharp;

            Config.Set("debug-mode", "false");
            Config.Set("parameters", JsonConvert.SerializeObject(parameters));
            Config.Set("data-folder", "Data");
            Config.Set("data-directory", "Data");
            Config.Set("cache-location", "Data");
            Config.Set("maximum-data-points-per-chart-series", "10000");

            AlgorithmManager algorithmManager = null;
            var statistics = new Dictionary<string, string>();
            var alphaStatistics = new AlphaRuntimeStatistics();
            BacktestingResultHandler results = null;

            Composer.Instance.Reset();
            SymbolCache.Clear();
            MarketOnCloseOrder.SubmissionTimeBuffer = MarketOnCloseOrder.DefaultSubmissionTimeBuffer;

            try
            {
                // set the configuration up
                Config.Set("algorithm-type-name", algorithm);
                Config.Set("live-mode", "false");
                Config.Set("environment", "");
                Config.Set("messaging-handler", "QuantConnect.Messaging.Messaging");
                Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
                Config.Set("setup-handler", setupHandler);
                Config.Set("history-provider", "RegressionHistoryProviderWrapper");
                Config.Set("api-handler", "QuantConnect.Api.Api");
                Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.RegressionResultHandler");
                Config.Set("algorithm-language", language.ToString());
                Config.Set("algorithm-location", algorithmLocation);

                // Store initial log variables
                using var algorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
                using var systemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
                using var workerThread = new TestWorkerThread();
                Log.Trace("");
                Log.Trace("{0}: Running " + algorithm + "...", DateTime.UtcNow);
                Log.Trace("");


                // run the algorithm in its own thread
                var engine = new QuantConnect.Lean.Engine.Engine(systemHandlers, algorithmHandlers, false);
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var job = (BacktestNodePacket)systemHandlers.JobQueue.NextJob(out string algorithmPath);
                        job.BacktestId = algorithm;
                        job.PeriodStart = startDate;
                        job.PeriodFinish = endDate;
                        if (initialCash.HasValue)
                        {
                            job.CashAmount = new CashAmount(initialCash.Value, Currencies.USD);
                        }
                        algorithmManager = new AlgorithmManager(false, job);

                        systemHandlers.LeanManager.Initialize(systemHandlers, algorithmHandlers, job, algorithmManager);

                        engine.Run(job, algorithmManager, algorithmPath, workerThread);
                    }
                    catch (Exception e)
                    {
                        Log.Trace($"Error in AlgorithmRunner task: {e}");
                    }
                }).Wait();

                var backtestingResultHandler = (BacktestingResultHandler)algorithmHandlers.Results;
                results = backtestingResultHandler;
                statistics = backtestingResultHandler.FinalStatistics;

                var defaultAlphaHandler = (DefaultAlphaHandler)algorithmHandlers.Alphas;
                alphaStatistics = defaultAlphaHandler.RuntimeStatistics;
            }
            catch (Exception ex)
            {
                if (expectedFinalStatus != AlgorithmStatus.RuntimeError)
                {
                    Log.Error("{0} {1}", ex.Message, ex.StackTrace);
                }
            }

            if (algorithmManager?.State != expectedFinalStatus)
            {
                Assert.Fail($"Algorithm state should be {expectedFinalStatus} and is: {algorithmManager?.State}");
            }

            foreach (var expectedStat in expectedStatistics)
            {
                Assert.IsTrue(statistics.TryGetValue(expectedStat.Key, out string result), "Missing key: " + expectedStat.Key);

                // normalize -0 & 0, they are the same thing
                var expected = expectedStat.Value;
                if (expected == "-0")
                {
                    expected = "0";
                }

                if (result == "-0")
                {
                    result = "0";
                }

                Assert.AreEqual(expected, result, "Failed on " + expectedStat.Key);
            }

            // Use reflection to get protected property
            var privateResult = new PrivateObject(results);
            var runtimeStatistics = (Dictionary<string, string>)privateResult.GetProperty("RuntimeStatistics");
            Assert.IsNotNull(runtimeStatistics);
            Assert.IsNotNull(results.FinalStatistics);
            return runtimeStatistics.Concat(results.FinalStatistics).ToDictionary();
        }

        private static void AssertAlphaStatistics(AlphaRuntimeStatistics expected, AlphaRuntimeStatistics actual, Expression<Func<AlphaRuntimeStatistics, object>> selector)
        {
            // extract field name from expression
            var field = selector.AsEnumerable().OfType<MemberExpression>().First().ToString();
            field = field[(field.IndexOf('.') + 1)..];

            var func = selector.Compile();
            var expectedValue = func(expected);
            var actualValue = func(actual);
            if (expectedValue is double value)
            {
                Assert.AreEqual(value, (double)actualValue, 1e-4, "Failed on alpha statistics " + field);
            }
            else
            {
                Assert.AreEqual(expectedValue, actualValue, "Failed on alpha statistics " + field);
            }
        }
    }

    internal class TestAccountCurrencyProvider : IAccountCurrencyProvider
    {
        public string AccountCurrency { get; }

        public TestAccountCurrencyProvider() : this(Currencies.USD) { }

        public TestAccountCurrencyProvider(string accountCurrency)
        {
            AccountCurrency = accountCurrency;
        }
    }
}
