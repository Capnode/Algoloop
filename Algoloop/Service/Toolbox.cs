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
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;

namespace Algoloop.Service
{
    public class Toolbox : MarshalByRefObject
    {
        public bool Run(MarketModel marketModel)
        {
            if (!SetConfig(marketModel))
            {
                return false;
            }

            Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

            try
            {

            }
            catch (Exception ex)
            {
                Log.LogHandler.Error("{0}: {1}", ex.GetType(), ex.Message);
                return false;
            }
            finally
            {
                Log.LogHandler.Dispose();
            }

            return true;
        }

        private bool SetConfig(MarketModel marketModel)
        {
            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-folder", "../../../Data/");
            Config.Set("fxcm-terminal", Enum.GetName(typeof(AccountModel.AccountType), marketModel.Type));
            Config.Set("fxcm-user-name", marketModel.Login);
            Config.Set("fxcm-password", marketModel.Password);
            return true;
        }
    }
}
