/*
 * Copyright 2018 Capnode AB
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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Algoloop.Model
{
    public static class MainService
    {
        public static string GetProgramFolder()
        {
            string unc = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(unc);
            return folder;
        }

        public static string GetAppDataFolder()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string company = AboutModel.AssemblyCompany.Split(' ')[0];
            string product = AboutModel.AssemblyProduct;
            string path = Path.Combine(appData, company, product);
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetProgramDataFolder()
        {
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string company = AboutModel.AssemblyCompany.Split(' ')[0];
            string product = AboutModel.AssemblyProduct;
            string path = Path.Combine(programData, company, product);
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetUserDataFolder()
        {
            string userData = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string company = AboutModel.AssemblyCompany.Split(' ')[0];
            string product = AboutModel.AssemblyProduct;
            string path = Path.Combine(userData, company, product);
            Directory.CreateDirectory(path);
            return path;
        }

        public static void CopyDirectory(string sourceDir, string destDir, bool overwiteFiles)
        {
            if (!Directory.Exists(sourceDir))
            {
                Log.Error($"Source directory {sourceDir} does not exist");
                return;
            }

            // Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));

            // Copy all the files & Replaces any files with the same name
            foreach (string source in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                string dest = source.Replace(sourceDir, destDir);
                if (!File.Exists(dest) || overwiteFiles)
                {
                    File.Copy(source, dest, true);
                }
            }
        }

        public static string FullExePath(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            if (File.Exists(file)) return file;
            string path = Path.Combine(GetProgramFolder(), file);
            if (File.Exists(path)) return path;
            return null;
        }

        public static int DbVersion(string json)
        {
            int version = 0;
            var regex = new Regex(@"^{\s*""Version"":\s*(\d+)", RegexOptions.IgnoreCase);
            Match match = regex.Match(json);
            if (match.Success)
            {
                version = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            return version;
        }

        public static void Delete(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static void DeleteFolders(string path, string pattern)
        {
            foreach (string folder in Directory.EnumerateDirectories(path, pattern))
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
