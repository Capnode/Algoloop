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
using QuantConnect.ToolBox.KrakenDownloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Algoloop.ViewModel.Internal.Provider
{
    internal class Kraken : ProviderBase
    {
        public override void GetUpdate(ProviderModel provider, Action<object> update, CancellationToken cancel)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-directory", Globals.DataFolder);
            Config.Set("cache-location", Globals.DataFolder);

            IList<string> symbols = provider.Symbols.Select(m => m.Id).ToList();
            string resolution = Resolution.Daily.ToString(); // Yahoo only support daily
            KrakenDownloaderProgram.KrakenDownloader(symbols, resolution, provider.LastDate, provider.LastDate);
        }
    }
}
