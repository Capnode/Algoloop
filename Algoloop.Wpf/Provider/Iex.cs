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

namespace Algoloop.Wpf.Provider
{
    public class Iex : ProviderBase
    {
        private SettingModel _settings;

        public override bool Register(SettingModel settings)
        {
            _settings = settings;
            return base.Register(settings);
        }

        public override IReadOnlyList<SymbolModel> GetMarketData(ProviderModel model, Action<object> update)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            
            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-directory", _settings.DataFolder);

            IList<string> symbols = model.Symbols.Select(m => m.Id).ToList();
            string resolution = Resolution.Daily.ToString(); // Yahoo only support daily
            IEXDownloaderProgram.IEXDownloader(symbols, resolution, model.LastDate, model.LastDate);
            return null;
        }
    }
}
