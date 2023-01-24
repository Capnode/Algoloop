/*
 * Copyright 2020 Capnode AB
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
using Algoloop.ViewModel.Internal;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using QuantConnect;
using System.Reflection;

namespace Algoloop.ViewModel
{
    public class ResearchViewModel : ViewModelBase, IDisposable
    {
        private const string InstallPythonPage = @"https://github.com/Capnode/Algoloop/wiki/Install-Python-and-Jupyter-Lab";
        private const string RuntimeConfig = "QuantConnect.Lean.Launcher.runtimeconfig.json";

        private readonly SettingModel _settings;
        private string _htmlText;
        private string _source;
        private ConfigProcess _process;
        private bool _disposed;
        private readonly string[] _exeFiles =
        {
            "start.py",
            "Initialize.csx",
            "QuantConnect.csx",
            "Accord.Fuzzy.dll",
            "Accord.MachineLearning.dll",
            "Accord.Math.Core.dll",
            "Accord.Math.dll",
            "Accord.Statistics.dll",
            "Accord.dll",
            "AsyncIO.dll",
            "CloneExtensions.dll",
            "CoinAPI.WebSocket.V1.dll",
            "Common.Logging.Core.dll",
            "Common.Logging.dll",
            "CsvHelper.dll",
            "DotNetZip.dll",
            "DynamicInterop.dll",
            "FSharp.Core.dll",
            "Fasterflect.dll",
            "ICSharpCode.SharpZipLib.dll",
            "IQFeed.CSharpApiClient.dll",
            "LaunchDarkly.EventSource.dll",
            "MathNet.Numerics.dll",
            "McMaster.Extensions.CommandLineUtils.dll",
            "Microsoft.IO.RecyclableMemoryStream.dll",
            "Microsoft.Windows.SDK.NET.dll",
            "NaCl.dll",
            "NetMQ.dll",
            "Newtonsoft.Json.dll",
            "NodaTime.dll",
            "Python.Runtime.dll",
            "QLNet.dll",
            "QuantConnect.Algorithm.CSharp.dll",
            "QuantConnect.Algorithm.Framework.dll",
            "QuantConnect.Algorithm.dll",
            "QuantConnect.AlgorithmFactory.dll",
            "QuantConnect.Api.dll",
            "QuantConnect.Brokerages.dll",
            "QuantConnect.Common.dll",
            "QuantConnect.Compression.dll",
            "QuantConnect.Configuration.dll",
            "QuantConnect.Indicators.dll",
            "QuantConnect.Lean.Engine.dll",
            "QuantConnect.Lean.Launcher.dll",
            "QuantConnect.Logging.dll",
            "QuantConnect.Messaging.dll",
            "QuantConnect.Queues.dll",
            "QuantConnect.Research.dll",
            "QuantConnect.ToolBox.dll",
            "RDotNet.dll",
            "RestSharp.dll",
            "System.ComponentModel.Composition.dll",
            "System.Private.ServiceModel.dll",
            "System.ServiceModel.Primitives.dll",
            "System.ServiceModel.dll",
            "Utf8Json.dll",
            "WinRT.Runtime.dll",
            "protobuf-net.Core.dll",
        };

        public ResearchViewModel(SettingModel settings)
        {
            _settings = settings;
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public string HtmlText
        {
            get => _htmlText;
            set => SetProperty(ref _htmlText, value);
        }

        public string Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                // Dispose managed state (managed objects).
                _process?.Dispose();
            }

            _disposed = true;
        }

        public void Initialize()
        {
            try
            {
                StopJupyter();
                _process = new ConfigProcess(
                    "jupyter.exe",
                    $"lab --no-browser",
                    _settings.Notebook,
                    false,
                    (line) => Log.Trace(line),
                    (line) =>
                    {
                        if (string.IsNullOrEmpty(line)) return;

                        int pos = line.IndexOf("http", StringComparison.OrdinalIgnoreCase);
                        if (string.IsNullOrEmpty(Source) && pos > 0)
                        {
                            Source = line[pos..];
                        }

                        Log.Trace(line);
                    });

                // Setup Python and Jupyter environment
                string exeFolder = MainService.GetProgramFolder();
                SetNotebookFolder(exeFolder);
                PythonSupport.SetupPython(_process.Environment, exeFolder);

                // Set config file
                IDictionary<string, string> config = _process.Config;
                config["job-user-id"] = int.TryParse(_settings.ApiUser, out _) ? _settings.ApiUser : "0";
                config["api-access-token"] = _settings.ApiToken;
                config["algorithm-language"] = Language.Python.ToString();
                config["data-folder"] = _settings.DataFolder.Replace("\\", "/");
                config["data-directory"] = _settings.DataFolder.Replace("\\", "/");
                config["composer-dll-directory"] = exeFolder.Replace("\\", "/");
                config["plugin-directory"] = exeFolder.Replace("\\", "/");
                config["log-handler"] = "QuantConnect.Logging.CompositeLogHandler";
                config["messaging-handler"] = "QuantConnect.Messaging.Messaging";
                config["job-queue-handler"] = "QuantConnect.Queues.JobQueue";
                config["api-handler"] = "QuantConnect.Api.Api";
                config["map-file-provider"] = "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider";
                config["factor-file-provider"] = "QuantConnect.Data.Auxiliary.LocalDiskFactorFileProvider";
                config["data-provider"] = "QuantConnect.Lean.Engine.DataFeeds.DefaultDataProvider";
                config["alpha-handler"] = "QuantConnect.Lean.Engine.Alphas.DefaultAlphaHandler";
                config["data-channel-provider"] = "DataChannelProvider";
                config["object-store"] = "QuantConnect.Lean.Engine.Storage.LocalObjectStore";
                config["data-aggregator"] = "QuantConnect.Lean.Engine.DataFeeds.AggregationManager";

                // Start process
                _process.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Source = InstallPythonPage;
                _process = null;
            }
        }

        public void StopJupyter()
        {
            if (_process == null) return;

            bool stopped = _process.Abort();
            Log.Trace($"Jupyter process exit: {stopped}");
            Debug.Assert(stopped);
            _process.Dispose();
            _process = null;
        }

        /// <summary>
        /// Convert runtimeconfig.json file to a format acceptable to Python CLR loader
        /// </summary>
        internal static void CopyRuntimeConfig(string sourceFile, string destFile)
        {
            string json = File.ReadAllText(sourceFile);
            JObject root = JObject.Parse(json);
            JObject runtimeOptions = root["runtimeOptions"] as JObject;
            JToken frameworks = runtimeOptions!["includedFrameworks"];
            if (frameworks != null)
            {
                runtimeOptions["framework"] = frameworks.First();
                runtimeOptions.Remove("includedFrameworks");
            }

            using StreamWriter file = File.CreateText(destFile);
            using JsonTextWriter writer = new(file);
            writer.Formatting = Formatting.Indented;
            root.WriteTo(writer);
        }

        private void SetNotebookFolder(string exeFolder)
        {
            DirectoryInfo notebook = Directory.CreateDirectory(_settings.Notebook);
            string parent = notebook.Parent!.FullName;
            CopyExeFiles(exeFolder, parent);

            string sourceFile = Path.Combine(exeFolder, RuntimeConfig);
            string destFile = Path.Combine(parent, RuntimeConfig);
            CopyRuntimeConfig(sourceFile, destFile);
        }

        private void CopyExeFiles(string exeFolder, string notebookFolder)
        {
            foreach (string filename in _exeFiles)
            {
                // Copy file if newer
                FileInfo sourceFile = new(Path.Combine(exeFolder, filename));
                if (!sourceFile.Exists)
                {
                    Log.Error($"{MethodBase.GetCurrentMethod()!.DeclaringType!.FullName}.{MethodBase.GetCurrentMethod()!.Name}: {filename} does not exist");
                    continue;
                }

                FileInfo destFile = new(Path.Combine(notebookFolder, filename));
                if (sourceFile.LastWriteTime > destFile.LastWriteTime)
                {
                    File.Copy(sourceFile.FullName, destFile.FullName, true);
                }
            }
        }
    }
}
