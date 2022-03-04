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

/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:Algoloop"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"
*/

using Algoloop.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using System.Diagnostics;

namespace Algoloop.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            Debug.Assert(ViewModelBase.IsUiThread(), "Not UI thread!");

            // Register Algoloop types
            var services = new ServiceCollection();
            services.AddSingleton<SettingModel>();
            services.AddSingleton<MarketsModel>();
            services.AddSingleton<StrategiesModel>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MarketsViewModel>();
            services.AddSingleton<StrategiesViewModel>();
            services.AddSingleton<ResearchViewModel>();
            services.AddSingleton<LogViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<AboutViewModel>();
            Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        }

        public static MainViewModel MainViewModel =>
            Ioc.Default.GetService<MainViewModel>();
        public static MarketsViewModel MarketsViewModel =>
            Ioc.Default.GetService<MarketsViewModel>();
        public static StrategiesViewModel StrategiesViewModel =>
            Ioc.Default.GetService<StrategiesViewModel>();
        public static ResearchViewModel ResearchViewModel =>
            Ioc.Default.GetService<ResearchViewModel>();
        public static LogViewModel LogViewModel =>
            Ioc.Default.GetService<LogViewModel>();
        public static SettingsViewModel SettingsViewModel =>
            Ioc.Default.GetService<SettingsViewModel>();
        public static AboutViewModel AboutViewModel =>
            Ioc.Default.GetService<AboutViewModel>();

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}
