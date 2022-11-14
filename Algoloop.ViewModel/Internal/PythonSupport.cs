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

namespace Algoloop.ViewModel.Internal
{
    internal class PythonSupport
    {
        private const string ExePath = "PATH";
        private const string PythonPath = "PYTHONPATH";
        private const string PythonHome = "PYTHONHOME";
        private const string PythonnetPyDll = "PYTHONNET_PYDLL";
        private const string PythonPattern = "python3?.dll";
        private const string SitePackages = @"Lib\site-packages";

        public static void SetupPython(StringDictionary environment, string exeFolder)
        {
            string paths = environment[ExePath];
            Log.Trace($"Env[{ExePath}] = {paths}");
            foreach (string folder in paths.Split(";"))
            {
                if (!Directory.Exists(folder))
                    continue;

                var dlls = Directory.EnumerateFiles(folder, PythonPattern);
                foreach (var dll in dlls)
                {
                    // Skip python3.dll
                    var info = new FileInfo(dll);
                    string name = info.Name;
                    if (name.Length < PythonPattern.Length)
                        continue;

                    string pythonpath = Path.Combine(folder, SitePackages) + ";" + exeFolder;
                    if (environment.ContainsKey(PythonPath))
                    {
                        pythonpath = pythonpath + ";" + environment[PythonPath];
                    }

                    environment[PythonPath] = pythonpath;
                    Log.Trace($"Env[{PythonPath}] = {pythonpath}");
                    environment[PythonnetPyDll] = dll;
                    Log.Trace($"Env[{PythonnetPyDll}] = {dll}");
                    environment[PythonHome] = folder;
                    Log.Trace($"Env[{PythonHome}] = {folder}");
                    return;
                }
            }

            throw new ApplicationException($"Python is not installed: {PythonPattern} not found");
        }
    }
}
