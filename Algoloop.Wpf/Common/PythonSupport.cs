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

using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace Algoloop.Wpf.Common
{
    public class PythonSupport
    {
        private const string _path = "PATH";
        private const string _pythonPath = "PYTHONPATH";
        private const string _pythonHome = "PYTHONHOME";
        private const string _pythonnetPyDll = "PYTHONNET_PYDLL";
        private const string _pythonDll = "python36.dll";

        public static void SetupPython(StringDictionary environment)
        {
            string paths = environment[_path];
            foreach (string folder in paths.Split(";"))
            {
                string pythonDll = Directory.EnumerateFiles(folder, _pythonDll).FirstOrDefault();
                if (pythonDll == default) continue;
                environment[_pythonnetPyDll] = pythonDll;
                environment[_pythonHome] = folder;
                return;
            }

            throw new ApplicationException($"Python is not installed: {_pythonDll} not found");
        }


        public static void SetupJupyter(StringDictionary environment, string exeFolder)
        {
            if (environment.ContainsKey(_pythonPath))
            {
                string pythonpath = environment[_pythonPath];
                environment[_pythonPath] = exeFolder + ";" + pythonpath;
            }
            else
            {
                environment[_pythonPath] = exeFolder;
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string sourceFile = Path.Combine(exeFolder, "start.py");
            string destFile = Path.Combine(home, @".ipython\profile_default\startup\quantconnect.py");
            File.Copy(sourceFile, destFile, true);

            sourceFile = Path.Combine(exeFolder, @"QuantConnect.Lean.Launcher.runtimeconfig.json");
            destFile = Path.Combine(home, @".ipython\profile_default\startup\QuantConnect.Lean.Launcher.runtimeconfig.json");
            File.Copy(sourceFile, destFile, true);
        }
    }
}
