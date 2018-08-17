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
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.ToolBox.FxcmDownloader;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Algoloop.Service
{
    public class Toolbox : MarshalByRefObject
    {
        public bool Run(MarketModel model)
        {
            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-folder", "../../../Data/");

            try
            {
                using (var writer = new StringWriter())
                {
                    Console.SetOut(writer);
                    IList<string> list = model.Symbols.Where(m => m.Enabled).Select(m => m.Name).ToList();
                    if (list.Any())
                    {
                        switch (model.Provider)
                        {
                            case MarketModel.DataProvider.Fxcm:
                                FxcmDownloader(model, list);
                                break;
                        }
                    }

                    writer.Flush();
                    var console = writer.GetStringBuilder().ToString();
                    Log.Trace(console);
                    return true;
                }
            }
            catch (Exception ex)
            {
                string log = string.Format("{0}: {1}", ex.GetType(), ex.Message);
                Log.Error(log);
                return false;
            }
        }

        private static void FxcmDownloader(MarketModel marketModel, IList<string> symbols)
        {
            Config.Set("fxcm-terminal", Enum.GetName(typeof(AccountModel.AccountType), marketModel.Type));
            Config.Set("fxcm-user-name", marketModel.Login);
            Config.Set("fxcm-password", marketModel.Password);

            string resolution = marketModel.Resolution.Equals(Resolution.Tick) ? "all" : marketModel.Resolution.ToString();
            FxcmDownloaderProgram.FxcmDownloader(symbols, resolution, marketModel.FromDate, marketModel.FromDate);
        }
    }
}
