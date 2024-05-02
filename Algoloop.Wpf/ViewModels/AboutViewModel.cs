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
using System.Reflection;

namespace Algoloop.Wpf.ViewModels
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

            // Set Lean Credits
            object[] attributes = Assembly
                .GetAssembly(typeof(Globals))
                .GetCustomAttributes(typeof(AssemblyCopyrightAttribute),
                false);
            if (attributes.Length > 0)
            {
                string copyright = ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
                Credit = "Lean " + Globals.Version + ", " + copyright;
            }

            Debug.Assert(IsUiThread(), "Not UI thread!");
        }
    }
}
