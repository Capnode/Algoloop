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
using QuantConnect;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Algoloop.ViewModel
{
    public class ResearchViewModel : ViewModelBase, IDisposable
    {
        private const string Notebook = "Notebook";
        private readonly SettingModel _settings;
        private string _htmlText;
        private string _source;
        private ConfigProcess _process;
        private bool _disposed;
        private bool _initialized;

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

        public bool Initialized
        {
            get => _initialized;
            private set => SetProperty(ref _initialized, value);
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
                SetNotebookFolder();
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
                Initialized = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Python must also be installed to use Research page.\nSee: https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#quantconnect-python-algorithm-project \n");
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

        private void SetNotebookFolder()
        {
            if (string.IsNullOrEmpty(_settings.Notebook))
            {
                string userDataFolder = MainService.GetUserDataFolder();
                _settings.Notebook = Path.Combine(userDataFolder, Notebook);
                Directory.CreateDirectory(_settings.Notebook);
            }
        }
    }
}
