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
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using QuantConnect.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Algoloop.Wpf.ViewModel
{
    public class SettingsViewModel : ViewModel
    {
        public SettingsViewModel(SettingModel settings)
        {
            Model = settings;
            OkCommand = new RelayCommand<Window>(window => DoOk(window));
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public RelayCommand<Window> OkCommand { get; private set; }

        public SettingModel Model { get; }

        public async Task<bool> ReadAsync(string fileName)
        {
            Log.Trace($"Reading {fileName}");
            if (File.Exists(fileName))
            {
                try
                {
                    using StreamReader r = new StreamReader(fileName);
                    string json = r.ReadToEnd();
                    SettingModel settings = JsonConvert.DeserializeObject<SettingModel>(json);
                    Model.Copy(settings);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed reading {fileName}\n");
                    return false;
                }
            }

            DataFromModel();
            return await Task.FromResult(true).ConfigureAwait(false);
        }

        internal bool Save(string fileName)
        {
            try
            {
                DataToModel();

                using StreamWriter file = File.CreateText(fileName);
                JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.Indented };
                serializer.Serialize(file, Model);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed writing {fileName}\n");
                return false;
            }
        }

        private void DoOk(Window window)
        {
            window.DialogResult = true;
            window.Close();
        }

        private void DataToModel()
        {
            Model.Version = SettingModel.version;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        private void DataFromModel()
        {
        }
    }
}
