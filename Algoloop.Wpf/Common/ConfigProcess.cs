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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Algoloop.Wpf.Common
{
    public class ConfigProcess: IDisposable
    {
        private const int CTRL_C_EVENT = 0;
        private const string _configfile = "config.json";
        private readonly string _workFolder;
        private readonly Process _process;
        private readonly IDictionary<string, string> _config = new Dictionary<string, string>();
        private bool _isDisposed;

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

        public ConfigProcess(string filename, string arguments, string workFolder, Action<string> output, Action<string> error)
        {
            _workFolder = workFolder;
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    WorkingDirectory = workFolder,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            _process.OutputDataReceived += (sender, args) => output(args.Data);
            _process.ErrorDataReceived += (sender, args) => error(args.Data);
        }

        public StringDictionary Environment => _process.StartInfo.EnvironmentVariables;
        public IDictionary<string, string> Config => _config;

        public void Start()
        {
            // Save config file
            Directory.CreateDirectory(_workFolder);
            using StreamWriter file = File.CreateText(Path.Combine(_workFolder, _configfile));
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented
            };
            serializer.Serialize(file, Config);

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public bool Stop()
        {
            // Send Ctrl-C to Jupyter console
            if (AttachConsole((uint)_process.Id))
            {
                SetConsoleCtrlHandler(null, true);
                try
                {
                    if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0)) return false;
                    _process.WaitForExit();
                }
                finally
                {
                    FreeConsole();
                    SetConsoleCtrlHandler(null, false);
                }
                _process.WaitForExit();
            }

            return _process.HasExited;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _process.Dispose();
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
