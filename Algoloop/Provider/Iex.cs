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
using Algoloop.Service;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.ToolBox.IEX;
using System.Collections.Generic;

namespace Algoloop.Provider
{
    class Iex : IProvider
    {
        public void Download(MarketModel model, SettingService settings, IList<string> symbols)
        {
            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-directory", settings.DataFolder);

            string resolution = Resolution.Daily.ToString(); // Yahoo only support daily
            IEXDownloaderProgram.IEXDownloader(symbols, resolution, model.LastDate, model.LastDate, model.ApiKey);
        }

        public IEnumerable<SymbolModel> GetAllSymbols(MarketModel market, SettingService settings)
        {
            throw new System.NotImplementedException();
        }
    }
}
