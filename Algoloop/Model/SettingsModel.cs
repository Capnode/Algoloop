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
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class SettingsModel
    {
        [DataMember]
        [DisplayName("Data folder")]
        [Description("Folder for market data.")]
        [Editor(typeof(FolderEditor), typeof(FolderEditor))]
        public string DataFolder { get; set; } = @"..\..\..\Data";

        internal void Copy(SettingsModel oldSettings)
        {
            DataFolder = oldSettings.DataFolder;
        }
    }
}
