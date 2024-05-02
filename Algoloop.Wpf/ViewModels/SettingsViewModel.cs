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

using Algoloop.Model;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using QuantConnect.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Algoloop.Wpf.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        const string SettingsFile = "Settings.json";
        const string BackupFile = "Settings.bak";
        const string TempFile = "Settings.tmp";

        public SettingsViewModel(SettingModel settings)
        {
            Model = settings;
            OkCommand = new RelayCommand<Window>(window => DoOk(window));
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public RelayCommand<Window> OkCommand { get; }

        public SettingModel Model { get; }

        internal bool Read(string folder)
        {
            if (!Directory.Exists(folder)) throw new ArgumentException($"Can not find Folder: {folder}");
            if (ReadFile(Path.Combine(folder,SettingsFile))) return true;
            if (ReadFile(Path.Combine(folder, BackupFile))) return true;
            return false;
        }

        internal void Save(string folder)
        {
            if (!Directory.Exists(folder)) throw new ArgumentException($"Can not find Folder: {folder}");
            string fileName = Path.Combine(folder, SettingsFile);
            string backupFile = Path.Combine(folder, BackupFile);
            string tempFile = Path.Combine(folder, TempFile);
            if (File.Exists(fileName))
            {
                File.Copy(fileName, tempFile, true);
            }

            DataToModel();
            SaveFile(fileName);
            if (File.Exists(tempFile))
            {
                File.Move(tempFile, backupFile, true);
            }
        }

        private bool ReadFile(string fileName)
        {
            if (!File.Exists(fileName)) return false;
            try
            {
                Log.Trace($"Reading {fileName}");
                using StreamReader r = new(fileName);
                string json = r.ReadToEnd();
                SettingModel settings = JsonConvert.DeserializeObject<SettingModel>(json);
                Model.Copy(settings);
                DataFromModel();
                return true;
            }
            catch (Exception ex)
            {
                Log.Trace($"Failed reading {fileName}: {ex.Message}", true);
                return false;
            }
        }

        private void SaveFile(string fileName)
        {
            Log.Trace($"Writing {fileName}");
            using StreamWriter file = File.CreateText(fileName);
            JsonSerializer serializer = new() { Formatting = Formatting.Indented };
            serializer.Serialize(file, Model);
        }

        private static void DoOk(Window window)
        {
            window.DialogResult = true;
            window.Close();
        }

        private void DataToModel()
        {
            Model.Version = SettingModel.ActualVersion;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        private void DataFromModel()
        {
        }
    }
}
