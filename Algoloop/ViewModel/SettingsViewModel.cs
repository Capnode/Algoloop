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

using Algoloop.ViewSupport;
using GalaSoft.MvvmLight.Command;
using System.ComponentModel;
using System.Windows;

namespace Algoloop.ViewModel
{
    public class SettingsViewModel
    {
        public SettingsViewModel()
        {
            OkCommand = new RelayCommand<Window>(window => OnOk(window));

            DataFolder = Properties.Settings.Default.DataFolder;
        }

        [DisplayName("Data folder")]
        [Description("Data folder location.")]
        [Editor(typeof(FolderEditor), typeof(FolderEditor))]
        public string DataFolder { get; set; }

        [Browsable(false)]
        public RelayCommand<Window> OkCommand { get; private set; }

        void OnOk(Window window)
        {
            Properties.Settings.Default.DataFolder = DataFolder;

            window.Close();
        }
    }
}
