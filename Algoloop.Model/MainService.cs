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
using System.IO;
using System.Reflection;

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
            string company = AboutModel.Company.Split(' ')[0];
            string product = AboutModel.Product;
            string path = Path.Combine(appData, company, product);
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetProgramDataFolder()
        {
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string company = AboutModel.Company.Split(' ')[0];
            string product = AboutModel.Product;
            string path = Path.Combine(programData, company, product);
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetUserDataFolder()
        {
            string userData = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string company = AboutModel.Company.Split(' ')[0];
            string product = AboutModel.Product;
            string path = Path.Combine(userData, company, product);
            Directory.CreateDirectory(path);
            return path;
        }

        public static void CopyDirectory(string sourceDir, string destDir, bool overwiteFiles = false)
        {
            if (!Directory.Exists(sourceDir))
            {
                Log.Error($"Source directory {sourceDir} does not exist");
                return;
            }

            // Create all of the directories
            Directory.CreateDirectory(destDir);
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

        public static string FullExePath(string location)
        {
            if (string.IsNullOrEmpty(location)) return null;
            string folder = Path.GetDirectoryName(location);
            string path = string.IsNullOrEmpty(folder) ? Path.Combine(MainService.GetProgramFolder(), location) : location;
            if (File.Exists(path)) return path;
            return null;
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
