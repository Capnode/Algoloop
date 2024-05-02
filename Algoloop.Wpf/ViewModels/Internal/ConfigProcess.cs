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
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Algoloop.Wpf.ViewModels.Internal
{
    internal class ConfigProcess: IDisposable
    {
        private const int CTRL_C_EVENT = 0;
        private const int Timeout = 10000;
        private const int MaxIndex = 65536;
        private const string Configfile = "config.json";
        private readonly string _workFolder;
        private readonly bool _useSubfolder;
        private readonly Process _process;
        private readonly IDictionary<string, string> _config = new Dictionary<string, string>();
        private bool _isDisposed;
        private static readonly object _lock = new();

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

        public ConfigProcess(
            string filename,
            string arguments,
            string workFolder,
            bool useSubfolder,
            Action<string> output,
            Action<string> error)
        {
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };
            _workFolder = workFolder;
            _useSubfolder = useSubfolder;
            _process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    output(args.Data);
                }
            };
            _process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    error(args.Data);
                }
            };
        }

        public StringDictionary Environment => _process.StartInfo.EnvironmentVariables;
        public IDictionary<string, string> Config => _config;

        public void Start()
        {
            // Set working folder
            string workFolder = CreateWorkFolder();
            _process.StartInfo.WorkingDirectory = workFolder;

            // Save config file
            using (StreamWriter file = File.CreateText(Path.Combine(workFolder, Configfile)))
            {
                JsonSerializer serializer = new() { Formatting = Formatting.Indented };
                serializer.Serialize(file, Config);
            }

            _process.Start();
            _process.PriorityClass = ProcessPriorityClass.BelowNormal;
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public bool Abort()
        {
            // Send Ctrl-C
            if (AttachConsole((uint)_process.Id))
            {
                SetConsoleCtrlHandler(null, true);
                try
                {
                    if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0)) return false;
                    if (_process.WaitForExit(Timeout)) return true;
                    Debug.Assert(!_process.HasExited);
                    _process.Kill();
                    if (_process.WaitForExit(Timeout)) return true;
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    return false;
                }
                finally
                {
                    FreeConsole();
                    SetConsoleCtrlHandler(null, false);
                }
            }

            return _process.HasExited;
        }

        public void WaitForExit(CancellationToken cancel, Action<string> postProcess = null)
        {
            try
            {
                _process.WaitForExitAsync(cancel).Wait();
            }
            catch (AggregateException aex)
            {
                if (aex.InnerException is TaskCanceledException)
                {
                    Abort();
                    cancel.ThrowIfCancellationRequested();
                }

                throw aex.InnerException;
            }
            finally
            {
                if (postProcess != null)
                {
                    string folder = _process.StartInfo.WorkingDirectory;
                    postProcess(folder);
                }
            }
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

        private string CreateWorkFolder()
        {
            if (_useSubfolder)
            {
                lock (_lock)
                {
                    for (int index = 0; index < MaxIndex; index++)
                    {
                        string folder = Path.Combine(_workFolder, $"temp{index}");
                        if (Directory.Exists(folder)) continue;
                        Directory.CreateDirectory(folder);
                        return folder;
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(_workFolder);
                return _workFolder;
            }

            throw new IOException("Can not create more temporary folders");
        }
    }
}
