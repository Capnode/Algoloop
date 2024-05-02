/*
 * Copyright 2019 Capnode AB
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
using QuantConnect.Configuration;
using QuantConnect.ToolBox.IEX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Algoloop.Wpf.ViewModels.Internal.Provider
{
    internal class Iex : ProviderBase
    {
        public override void GetUpdate(ProviderModel model, Action<object> update, CancellationToken cancel)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            
            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-directory", Globals.DataFolder);
            Config.Set("cache-location", Globals.DataFolder);

            IList<string> symbols = model.Symbols.Select(m => m.Id).ToList();
            string resolution = Resolution.Daily.ToString(); // Yahoo only support daily
            IEXDownloaderProgram.IEXDownloader(symbols, resolution, model.LastDate, model.LastDate);
        }
    }
}
