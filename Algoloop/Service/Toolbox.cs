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
using QuantConnect.ToolBox.DukascopyDownloader;
using QuantConnect.ToolBox.FxcmDownloader;
using QuantConnect.ToolBox.FxcmVolumeDownload;
using QuantConnect.ToolBox.GoogleDownloader;
using QuantConnect.ToolBox.IBDownloader;
using QuantConnect.ToolBox.IEX;
using QuantConnect.ToolBox.KrakenDownloader;
using QuantConnect.ToolBox.OandaDownloader;
using QuantConnect.ToolBox.QuandlBitfinexDownloader;
using QuantConnect.ToolBox.YahooDownloader;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Algoloop.Service
{
    public class Toolbox : MarshalByRefObject
    {
        public MarketModel Run(MarketModel model, HostDomainLogger logger)
        {
            Log.LogHandler = logger;
            PrepareDataFolder(model.DataFolder);

            using (var writer = new StreamLogger(logger))
            {
                Console.SetOut(writer);
                MarketDownloader(model);
            }

            Log.LogHandler.Dispose();
            return model;
        }

        private void PrepareDataFolder(string dataFolder)
        {
            string marketHoursFolder = Path.Combine(dataFolder, "market-hours");
            const string marketHoursFile = "market-hours-database.json";
            string marketHoursPath = Path.Combine(marketHoursFolder, marketHoursFile);
            if (!File.Exists(marketHoursPath))
            {
                Directory.CreateDirectory(marketHoursFolder);
                File.Copy(Path.Combine("Data", marketHoursFile), marketHoursPath);
            }

            string symbolPropertiesFolder = Path.Combine(dataFolder, "symbol-properties");
            const string symbolPropertiesFile = "symbol-properties-database.csv";
            string symbolPropertiesPath = Path.Combine(symbolPropertiesFolder, symbolPropertiesFile);
            if (!File.Exists(symbolPropertiesPath))
            {
                Directory.CreateDirectory(symbolPropertiesFolder);
                File.Copy(Path.Combine("Data", symbolPropertiesFile), symbolPropertiesPath);
            }
        }

        private void MarketDownloader(MarketModel model)
        {
            try
            {
                IList<string> list = model.Symbols.Where(m => m.Active).Select(m => m.Name).ToList();
                if (list.Any())
                {
                    switch (model.Provider)
                    {
                        case MarketModel.MarketType.CryptoIQ:
                            CryptoIQDownloader(model, list);
                            break;
                        case MarketModel.MarketType.Dukascopy:
                            DukascopyDownloader(model, list);
                            break;
                        case MarketModel.MarketType.Fxcm:
                            FxcmDownloader(model, list);
                            break;
                        case MarketModel.MarketType.FxcmVolume:
                            FxcmVolumeDownload(model, list);
                            break;
                        case MarketModel.MarketType.Gdax:
                            GdaxDownloader(model, list);
                            break;
                        case MarketModel.MarketType.Google:
                            GoogleDownloader(model, list);
                            break;
                        case MarketModel.MarketType.IB:
                            IBDownloader(model, list);
                            break;
                        case MarketModel.MarketType.IEX:
                            IEXDownloader(model, list);
                            break;
                        case MarketModel.MarketType.Kraken:
                            KrakenDownloader(model, list);
                            break;
                        case MarketModel.MarketType.Oanda:
                            OandaDownloader(model, list);
                            break;
                        case MarketModel.MarketType.QuandBitfinex:
                            QuandBitfinexDownloader(model, list);
                            break;
                        case MarketModel.MarketType.Yahoo:
                            YahooDownloader(model, list);
                            break;
                        default:
                            Log.Error($"Market Provider not supported: {model.Provider}");
                            model.Active = false;
                            break;
                    }
                }
                else
                {
                    Log.Trace($"No symbols selected");
                    model.Active = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("{0}: {1}", ex.GetType(), ex.Message));
                model.Active = false;
            }
        }

        private void CryptoIQDownloader(MarketModel model, IList<string> list)
        {
            throw new NotImplementedException();
        }

        private static void DukascopyDownloader(MarketModel model, IList<string> symbols)
        {
            Config.Set("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");
            Config.Set("data-directory", model.DataFolder);

            string resolution = model.Resolution.Equals(Resolution.Tick) ? "all" : model.Resolution.ToString();
            DateTime fromDate = model.FromDate.Date;
            if (fromDate < DateTime.Today)
            {
                DateTime nextDate = fromDate.AddDays(1);
                DukascopyDownloaderProgram.DukascopyDownloader(symbols, resolution, fromDate, nextDate.AddMilliseconds(-1));
                model.FromDate = nextDate;
            }
            model.Active = model.FromDate < DateTime.Today;
        }

        private static void FxcmDownloader(MarketModel model, IList<string> symbols)
        {
            Config.Set("map-file-provider", "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider");
            Config.Set("data-directory", model.DataFolder);
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
            DateTime fromDate = model.FromDate.Date;
            if (fromDate < DateTime.Today)
            {
                FxcmDownloaderProgram.FxcmDownloader(symbols, resolution, fromDate, fromDate);
                model.FromDate = fromDate.AddDays(1);
            }

            model.Active = model.FromDate < DateTime.Today;
        }

        private static void FxcmVolumeDownload(MarketModel model, IList<string> symbols)
        {
            Config.Set("data-directory", model.DataFolder);
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
            FxcmVolumeDownloadProgram.FxcmVolumeDownload(symbols, resolution, model.FromDate, model.FromDate);
        }

        private void GdaxDownloader(MarketModel model, IList<string> list)
        {
            throw new NotImplementedException();
        }

        private static void GoogleDownloader(MarketModel model, IList<string> symbols)
        {
            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-directory", model.DataFolder);

            string resolution = model.Resolution.Equals(Resolution.Tick) ? "all" : model.Resolution.ToString();
            GoogleDownloaderProgram.GoogleDownloader(symbols, resolution, model.FromDate, model.FromDate);
        }

        private void IBDownloader(MarketModel model, IList<string> symbols)
        {
            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-directory", model.DataFolder);

            string resolution = Resolution.Daily.ToString(); // Yahoo only support daily
            IBDownloaderProgram.IBDownloader(symbols, resolution, model.FromDate, model.FromDate);
        }

        private void IEXDownloader(MarketModel model, IList<string> symbols)
        {
            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-directory", model.DataFolder);

            string resolution = Resolution.Daily.ToString(); // Yahoo only support daily
            IEXDownloaderProgram.IEXDownloader(symbols, resolution, model.FromDate, model.FromDate);
        }

        private void KrakenDownloader(MarketModel model, IList<string> symbols)
        {
            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-directory", model.DataFolder);

            string resolution = Resolution.Daily.ToString(); // Yahoo only support daily
            KrakenDownloaderProgram.KrakenDownloader(symbols, resolution, model.FromDate, model.FromDate);
        }

        private void OandaDownloader(MarketModel model, IList<string> symbols)
        {
            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-directory", model.DataFolder);

            string resolution = Resolution.Daily.ToString(); // Yahoo only support daily
            OandaDownloaderProgram.OandaDownloader(symbols, resolution, model.FromDate, model.FromDate);
        }

        private void QuandBitfinexDownloader(MarketModel model, IList<string> symbols)
        {
            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-directory", model.DataFolder);

            string apiKey = ""; // TODO:
            QuandlBitfinexDownloaderProgram.QuandlBitfinexDownloader(model.FromDate, apiKey);
        }

        private static void YahooDownloader(MarketModel model, IList<string> symbols)
        {
            Config.Set("log-handler", "QuantConnect.Logging.CompositeLogHandler");
            Config.Set("data-directory", model.DataFolder);

            string resolution = Resolution.Daily.ToString(); // Yahoo only support daily
            YahooDownloaderProgram.YahooDownloader(symbols, resolution, model.FromDate, model.FromDate);
        }
    }
}
