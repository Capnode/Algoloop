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
using QuantConnect;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Algoloop.ViewModel
{
    public class ResearchViewModel : ViewModelBase, IDisposable
    {
        private const string Notebook = "Notebook";
        private const string InstallPythonPage = @"https://github.com/Capnode/Algoloop/wiki/Install-Python-and-Jupyter-Lab";
        private const string RuntimeConfig = "QuantConnect.Lean.Launcher.runtimeconfig.json";
        private const string StartPy = "start.py";
        private const string InitializeCsx = "Initialize.csx";
        private const string QuantConnectCsx = "QuantConnect.csx";

        private readonly SettingModel _settings;
        private string _htmlText;
        private string _source;
        private ConfigProcess _process;
        private bool _disposed;

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
                if (_process != null)
                {
                    _process.Dispose();
                }
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
                PythonSupport.SetupJupyter(_process.Environment, exeFolder);
                PythonSupport.SetupPython(_process.Environment);

                // Set config file
                IDictionary<string, string> config = _process.Config;
                config["algorithm-language"] = Language.Python.ToString();
                config["composer-dll-directory"] = exeFolder.Replace("\\", "/");
                config["data-folder"] = _settings.DataFolder.Replace("\\", "/");
                config["api-handler"] = "QuantConnect.Api.Api";
                config["job-queue-handler"] = "QuantConnect.Queues.JobQueue";
                config["messaging-handler"] = "QuantConnect.Messaging.Messaging";

                // Start process
                _process.Start();
            }
            catch (ApplicationException ex)
            {
                Log.Error(ex);
                Source = InstallPythonPage;
                _process = null;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                _process = null;
            }
        }

        public void StopJupyter()
        {
            if (_process == null) return;

            bool stopped = _process.Abort();
            Debug.Assert(stopped);
            Log.Trace($"Jupyter process exit: {stopped}");
            _process.Dispose();
            _process = null;
        }

        private void SetNotebookFolder(string exeFolder)
        {
            if (string.IsNullOrEmpty(_settings.Notebook))
            {
                string userDataFolder = MainService.GetUserDataFolder();
                _settings.Notebook = Path.Combine(userDataFolder, Notebook);
            }

            DirectoryInfo notebook = Directory.CreateDirectory(_settings.Notebook);
            string parent = notebook.Parent.FullName;

            string sourceFile = Path.Combine(exeFolder, StartPy);
            string destFile = Path.Combine(parent, StartPy);
            File.Copy(sourceFile, destFile, true);

            sourceFile = Path.Combine(exeFolder, InitializeCsx);
            destFile = Path.Combine(parent, InitializeCsx);
            File.Copy(sourceFile, destFile, true);

            sourceFile = Path.Combine(exeFolder, QuantConnectCsx);
            destFile = Path.Combine(parent, QuantConnectCsx);
            string content = File.ReadAllText(sourceFile);
            content = content.Replace("#r \"", $"#r \"{exeFolder}\\");
            File.WriteAllText(destFile, content);

            sourceFile = Path.Combine(exeFolder, RuntimeConfig);
            destFile = Path.Combine(parent, RuntimeConfig);
            CopyRuntimeConfig(sourceFile, destFile);
        }

        /// <summary>
        /// Convert runtimeconfig.json file to a format acceptable to Python CLR loader
        /// </summary>
        internal static void CopyRuntimeConfig(string sourceFile, string destFile)
        {
            string json = File.ReadAllText(sourceFile);
            JObject root = JObject.Parse(json);
            JObject runtimeOptions = root["runtimeOptions"] as JObject;
            JToken frameworks = runtimeOptions["includedFrameworks"];
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
    }
}
