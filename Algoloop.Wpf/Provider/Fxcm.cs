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
using QuantConnect.ToolBox.FxcmDownloader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Algoloop.Provider
{
    public class Fxcm : IProvider
    {
        private bool _isDisposed;

        public void Register(SettingModel settings)
        {
        }

        public void Download(MarketModel model, SettingModel settings)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            Config.Set("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");
            Config.Set("data-directory", settings.DataFolder);
            switch (model.Access)
            {
                case MarketModel.AccessType.Demo:
                    Config.Set("fxcm-terminal", "Demo");
                    break;

                case MarketModel.AccessType.Real:
                    Config.Set("fxcm-terminal", "Real");
                    break;
            }

            Config.Set("fxcm-user-name", model.Login);
            Config.Set("fxcm-password", model.Password);

            string resolution = model.Resolution.Equals(Resolution.Tick) ? "all" : model.Resolution.ToString();
            DateTime fromDate = model.LastDate.Date;
            if (fromDate < DateTime.Today)
            {
                IList<string> symbols = model.Symbols.Select(m => m.Id).ToList();
                FxcmDownloaderProgram.FxcmDownloader(symbols, resolution, fromDate, fromDate);
                model.LastDate = fromDate.AddDays(1);
            }

            model.Active = model.LastDate < DateTime.Today;
        }

        public void Abort()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
