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

using Algoloop.Service;
using GalaSoft.MvvmLight;
using QuantConnect.Logging;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Algoloop.ViewModel
{
    public class ResearchViewModel : ViewModelBase
    {
        internal const int CTRL_C_EVENT = 0;
        [DllImport("kernel32.dll")]
        internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);
        // Delegate type to be used as the Handler Routine for SCCH
        delegate Boolean ConsoleCtrlDelegate(uint CtrlType);

        private readonly SettingService _settings;
        private string _htmlText;
        private string _source;
        private Process _process;

        public ResearchViewModel(SettingService settings)
        {
            _settings = settings;
        }

        ~ResearchViewModel()
        {
            StopJupyter();
        }

        public string HtmlText
        {
            get => _htmlText;
            set => Set(ref _htmlText, value);
        }

        public string Source
        {
            get => _source;
            set => Set(ref _source, value);
        }

        public void StartJupyter()
        {
            StopJupyter();

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "jupyter.exe",
                    Arguments = $"notebook --no-browser",
                    WorkingDirectory = _settings.Notebook,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            //_process.StartInfo.EnvironmentVariables["PYTHONPATH"] = AppDomain.CurrentDomain.BaseDirectory;
            //_process.StartInfo.EnvironmentVariables["PATH"] = AppDomain.CurrentDomain.BaseDirectory + ";" + _process.StartInfo.EnvironmentVariables["PATH"];
            _process.OutputDataReceived += (sender, args) => Log.Trace(args.Data);
            _process.ErrorDataReceived += (sender, args) =>
            {
                string line = args.Data;
                if (string.IsNullOrEmpty(line)) return;

                int pos = line.IndexOf("http");
                if (string.IsNullOrEmpty(Source) && pos > 0)
                {
                    Source = line.Substring(pos);
                }

                Log.Trace(line);
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void StopJupyter()
        {
            if (_process == null) return;

            // Send Ctrl-C to Jupyter console
            if (AttachConsole((uint)_process.Id))
            {
                SetConsoleCtrlHandler(null, true);
                try
                {
                    if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0)) return;
                    _process.WaitForExit();
                }
                finally
                {
                    FreeConsole();
                    SetConsoleCtrlHandler(null, false);
                }
            }

            Log.Trace($"Jupyter process exit: {_process.HasExited}");
            _process = null;
        }
    }
}
