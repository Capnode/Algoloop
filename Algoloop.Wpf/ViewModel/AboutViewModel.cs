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
using GalaSoft.MvvmLight;
using System;
using System.Globalization;

namespace Algoloop.Wpf.ViewModel
{
    public class AboutViewModel : ViewModelBase
    {
        public string Title { get; private set; }
        public string ProductName { get; private set; }
        public string Version { get; private set; }
        public string Copyright { get; private set; }
        public string Description { get; private set; }

        public AboutViewModel()
        {
            Title = String.Format(CultureInfo.InvariantCulture, "About {0}", AboutModel.AssemblyProduct);
            ProductName = AboutModel.AssemblyProduct;
            Version = String.Format(CultureInfo.InvariantCulture, "Version: {0}", AboutModel.AssemblyVersion);
            Copyright = AboutModel.AssemblyCopyright;
            Description = AboutModel.AssemblyDescription;
        }
    }
}
