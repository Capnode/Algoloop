/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * Modifications Copyright (C) 2022 Capnode AB
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;
using SevenZip.Compression.LZMA;

namespace Algoloop.Wpf.ToolBox.DukascopyDownloader
{
    /// <summary>
    /// Dukascopy Data Downloader class
    /// </summary>
    public class DukascopyDataDownloader : IDataDownloader
    {
        private readonly DukascopySymbolMapper _symbolMapper = new();
        private const int DukascopyTickLength = 20;

        /// <summary>
        /// Checks if downloader can get the data for the symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns>Returns true if the symbol is available</returns>
        public static bool HasSymbol(string symbol)
        {
            return DukascopySymbolMapper.IsKnownLeanSymbol(Symbol.Create(symbol, GetSecurityType(symbol), Market.Dukascopy));
        }

        /// <summary>
        /// Gets the security type for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The security type</returns>
        public static SecurityType GetSecurityType(string symbol)
        {
            return DukascopySymbolMapper.GetLeanSecurityType(symbol);
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="dataDownloaderGetParameters">model class for passing in parameters for historical data</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
        {
            var symbol = dataDownloaderGetParameters.Symbol;
            var resolution = dataDownloaderGetParameters.Resolution;
            var startUtc = dataDownloaderGetParameters.StartUtc;
            var endUtc = dataDownloaderGetParameters.EndUtc;
            var tickType = dataDownloaderGetParameters.TickType;

            if (tickType != TickType.Quote)
            {
                yield break;
            }

            if (!DukascopySymbolMapper.IsKnownLeanSymbol(symbol)) throw new ArgumentException("Invalid symbol requested: " + symbol.Value);
            if (symbol.ID.SecurityType != SecurityType.Forex && symbol.ID.SecurityType != SecurityType.Cfd) throw
                    new NotSupportedException("SecurityType not available: " + symbol.ID.SecurityType);

            if (endUtc < startUtc) throw new ArgumentException("The end date must be greater or equal to the start date.");

            // set the starting date
            DateTime date = startUtc;

            // loop until last date
            while (date <= endUtc)
            {
                // request all ticks for a specific date
                var ticks = DownloadTicks(symbol, date);

                switch (resolution)
                {
                    case Resolution.Tick:
                        foreach (var tick in ticks)
                        {
                            yield return new Tick(tick.Time, symbol, tick.BidPrice, tick.AskPrice);
                        }
                        break;

                    case Resolution.Second:
                    case Resolution.Minute:
                    case Resolution.Hour:
                    case Resolution.Daily:
                        foreach (var bar in LeanData.AggregateTicks(ticks, symbol, resolution.ToTimeSpan()))
                        {
                            yield return bar;
                        }
                        break;
                }

                date = date.AddDays(1);
            }
        }

        /// <summary>
        /// Downloads all ticks for the specified date
        /// </summary>
        /// <param name="symbol">The requested symbol</param>
        /// <param name="date">The requested date</param>
        /// <returns>An enumerable of ticks</returns>
        private IEnumerable<Tick> DownloadTicks(Symbol symbol, DateTime date)
        {
            var dukascopySymbol = _symbolMapper.GetBrokerageSymbol(symbol);
            var pointValue = DukascopySymbolMapper.GetPointValue(symbol);

            for (var hour = 0; hour < 24; hour++)
            {
                var timeOffset = hour * 3600000;

                var url = $"http://www.dukascopy.com/datafeed/{dukascopySymbol}/" +
                          $"{date.Year.ToStringInvariant("D4")}/{(date.Month - 1).ToStringInvariant("D2")}/" +
                          $"{date.Day.ToStringInvariant("D2")}/{hour.ToStringInvariant("D2")}h_ticks.bi5";

                using var client = new HttpClient();
                while (true)
                {
                    byte[] bytes = null;
                    try
                    {
                        bytes = client.GetByteArrayAsync(url).Result;
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException is HttpRequestException rex)
                        {
                            if (rex.StatusCode == HttpStatusCode.NotFound)
                            {
                                yield break;
                            }
                        }

                        Log.Trace($"{ex.GetType()}: {url} {ex.Message}");
                        Thread.Sleep(60000);
                        continue;
                    }
                    if (bytes != null && bytes.Length > 0)
                    {
                        var ticks = AppendTicksToList(symbol, bytes, date, timeOffset, pointValue);
                        foreach (var tick in ticks)
                        {
                            yield return tick;
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Reads ticks from a Dukascopy binary buffer into a list
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="bytesBi5">The buffer in binary format</param>
        /// <param name="date">The date for the ticks</param>
        /// <param name="timeOffset">The time offset in milliseconds</param>
        /// <param name="pointValue">The price multiplier</param>
        private static unsafe List<Tick> AppendTicksToList(Symbol symbol, byte[] bytesBi5, DateTime date, int timeOffset, double pointValue)
        {
            var ticks = new List<Tick>();
            byte[] bytes = Decompress(bytesBi5);
            int count = bytes.Length / DukascopyTickLength;

            // Numbers are big-endian
            // ii1 = milliseconds within the hour
            // ii2 = AskPrice * point value
            // ii3 = BidPrice * point value
            // ff1 = AskVolume (not used)
            // ff2 = BidVolume (not used)

            fixed (byte* pBuffer = &bytes[0])
            {
                uint* p = (uint*)pBuffer;

                for (int i = 0; i < count; i++)
                {
                    ReverseBytes(p); uint time = *p++;
                    ReverseBytes(p); uint ask = *p++;
                    ReverseBytes(p); uint bid = *p++;
                    p++; p++;

                    if (bid > 0 && ask > 0)
                    {
                        ticks.Add(new Tick(
                            date.AddMilliseconds(timeOffset + time),
                            symbol,
                            Convert.ToDecimal(bid / pointValue),
                            Convert.ToDecimal(ask / pointValue)));
                    }
                }
            }

            return ticks;
        }

        private static byte[] Decompress(byte[] inputBytes)
        {
            MemoryStream newInStream = new(inputBytes);
            Decoder decoder = new();
            newInStream.Seek(0, 0);
            MemoryStream newOutStream = new();
            byte[] properties2 = new byte[5];
            if (newInStream.Read(properties2, 0, 5) != 5) throw (new Exception("input .lzma is too short"));
            long outSize = 0;
            for (int i = 0; i < 8; i++)
            {
                int v = newInStream.ReadByte();
                if (v < 0) throw (new Exception("Can't Read 1"));
                outSize |= ((long)(byte)v) << (8 * i);
            }
            decoder.SetDecoderProperties(properties2);
            long compressedSize = newInStream.Length - newInStream.Position;
            decoder.Code(newInStream, newOutStream, compressedSize, outSize, null);
            byte[] b = newOutStream.ToArray();
            return b;
        }

        /// <summary>
        /// Converts a 32-bit unsigned integer from big-endian to little-endian (and vice-versa)
        /// </summary>
        /// <param name="p">Pointer to the integer value</param>
        private static unsafe void ReverseBytes(uint* p)
        {
            *p = (*p & 0x000000FF) << 24 | (*p & 0x0000FF00) << 8 | (*p & 0x00FF0000) >> 8 | (*p & 0xFF000000) >> 24;
        }
    }
}
