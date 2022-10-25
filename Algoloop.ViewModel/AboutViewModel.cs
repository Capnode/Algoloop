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
using QuantConnect;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Algoloop.ViewModel
{
    public class AboutViewModel : ViewModelBase
    {
        public string Title { get; }
        public string ProductName { get; }
        public string Version { get; }
        public string Copyright { get; }
        public string Description { get; }
        public string Message { get; }
        public string Credit { get; }

        public AboutViewModel()
        {
            Title = String.Format(
                CultureInfo.InvariantCulture,
                "About {0}",
                AboutModel.Product);
            ProductName = AboutModel.Product;
            Version = String.Format(
                CultureInfo.InvariantCulture,
                "Version: {0}",
                AboutModel.Version);
            Copyright = AboutModel.Copyright;
            Description = AboutModel.Description;
            Message = string.Empty;
            Credit = "Lean " + Globals.Version + ", " + Globals.Copyright;
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }
    }
}
