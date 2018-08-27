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

using System;
using System.Reflection;
using System.Threading;

namespace Algoloop.Service
{
    internal class AppDomainService : IAppDomainService
    {
        private string _callingDomainName;
        private string _exeAssembly;
        private AppDomainSetup _ads;

        public AppDomainService()
        {
            _callingDomainName = Thread.GetDomain().FriendlyName;
            //Console.WriteLine(callingDomainName);

            // Get and display the full name of the EXE assembly.
            _exeAssembly = Assembly.GetEntryAssembly().FullName;
            //Console.WriteLine(exeAssembly);

            // Construct and initialize settings for a second AppDomain.
            _ads = new AppDomainSetup();
            _ads.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;

            _ads.DisallowBindingRedirects = false;
            _ads.DisallowCodeDownload = true;
            _ads.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
        }

        public AppDomain CreateAppDomain()
        {
            var name = Guid.NewGuid().ToString("x");
            return AppDomain.CreateDomain(name, null, _ads);
        }

        public T CreateInstance<T>(AppDomain ad)
        {
            return(T) ad.CreateInstanceAndUnwrap(_exeAssembly, typeof(T).FullName);
        }
    }
}
