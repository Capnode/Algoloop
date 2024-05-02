/*
 * Copyright 2021 Capnode AB
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

using QuantConnect.Logging;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace Algoloop.Wpf.ViewModels.Internal
{
    internal class PythonSupport
    {
        private const string ExePath = "PATH";
        private const string PythonPath = "PYTHONPATH";
        private const string PythonHome = "PYTHONHOME";
        private const string PythonnetPyDll = "PYTHONNET_PYDLL";
        private const string PythonPattern = "python3?.dll";

        public static void SetupPython(StringDictionary environment, string exeFolder)
        {
            string path = environment[ExePath];
            string[] paths = path.Trim(';').Split(";");
//            Log.Trace($"Env[{ExePath}] =");
//            paths.ToList().ForEach(m => Log.Trace($"  {m}"));

            foreach (string folder in paths)
            {
                if (!Directory.Exists(folder)) continue;

                var dlls = Directory.EnumerateFiles(folder, PythonPattern);
                foreach (var dll in dlls)
                {
                    // Skip python3.dll
                    var info = new FileInfo(dll);
                    string name = info.Name;
                    if (name.Length < PythonPattern.Length) continue;
                    environment[PythonnetPyDll] = dll;
                    Log.Trace($"Env[{PythonnetPyDll}] = {dll}");
                    environment[PythonHome] = folder;
                    Log.Trace($"Env[{PythonHome}] = {folder}");
                    environment[PythonPath] = exeFolder;
                    Log.Trace($"Env[{PythonPath}] = {exeFolder}");
                    return;
                }
            }

            throw new ApplicationException($"Python is not installed: {PythonPattern} not found");
        }
    }
}
