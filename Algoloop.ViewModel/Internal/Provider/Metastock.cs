/*
 * Copyright 2022 Capnode AB
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
using Algoloop.ToolBox.MetastockConverter;
using QuantConnect;
using System;
using System.Collections.Generic;

namespace Algoloop.ViewModel.Internal.Provider
{
    internal class Metastock : ProviderBase
    {
        public override void GetUpdate(ProviderModel market, Action<object> update)
        {
            if (market == null) throw new ArgumentNullException(nameof(market));
            if (!market.Resolution.Equals(Resolution.Daily)) throw new ArgumentException(nameof(market.Resolution));

            // Update symbol list
            IEnumerable<SymbolModel> actual = MetastockConverterProgram.GetInstruments(market.SourceFolder);
            UpdateSymbols(market, actual, update);

            // Setup convert parameters
            DateTime utsNow = DateTime.UtcNow;
            string[] args =
            {
                "--app=MetastockConverter",
                $"--source-dir={market.SourceFolder}",
                $"--destination-dir={Globals.DataFolder}",
            };
            IDictionary<string, string> config = new Dictionary<string, string>
            {
                ["data-directory"] = Globals.DataFolder,
                ["cache-location"] = Globals.DataFolder,
                ["data-folder"] = Globals.DataFolder
            };

            // Download active symbols
            RunProcess("Algoloop.ToolBox.exe", args, config);
            market.LastDate = utsNow.ToLocalTime();
            market.Active = false;
        }
    }
}
