/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
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
using System.Globalization;
using Newtonsoft.Json;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of using an external custom datasource. LEAN Engine is incredibly flexible and allows you to define your own data source.
    /// This includes any data source which has a TIME and VALUE. These are the *only* requirements. To demonstrate this we're loading in "Bitcoin" data.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="crypto" />
    public class CustomDataBitcoinAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            //Weather data we have is within these days:
            SetStartDate(2011, 9, 13);
            SetEndDate(DateTime.Now.Date.AddDays(-1));

            //Set the cash for the strategy:
            SetCash(100000);

            //Define the symbol and "type" of our generic data:
            AddData<Bitcoin>("BTC");
        }

        /// <summary>
        /// Event Handler for Bitcoin Data Events: These weather objects are created from our
        /// "Weather" type below and fired into this event handler.
        /// </summary>
        /// <param name="data">One(1) Weather Object, streamed into our algorithm synchronised in time with our other data streams</param>
        public void OnData(Bitcoin data)
        {
            //If we don't have any weather "SHARES" -- invest"
            if (!Portfolio.Invested)
            {
                //Weather used as a tradable asset, like stocks, futures etc.
                if (data.Close != 0)
                {
                    // It's only OK to use SetHoldings with crypto when using custom data. When trading with built-in crypto data, 
                    // use the cashbook. Reference https://github.com/QuantConnect/Lean/blob/master/Algorithm.Python/BasicTemplateCryptoAlgorithm.py 
                    SetHoldings("BTC", 1);
                }
                Console.WriteLine("Buying BTC 'Shares': BTC: " + data.Close);
            }
            Console.WriteLine("Time: " + Time.ToStringInvariant("T") + " " + Time.ToStringInvariant("T") + data.Close.ToStringInvariant());
        }

        /// <summary>
        /// Custom Data Type: Bitcoin data from Quandl - http://www.quandl.com/help/api-for-bitcoin-data
        /// </summary>
        public class Bitcoin : BaseData
        {
            [JsonProperty("timestamp")]
            public int Timestamp { get; set; }
            [JsonProperty("open")]
            public decimal Open { get; set; }
            [JsonProperty("high")]
            public decimal High { get; set; }
            [JsonProperty("low")]
            public decimal Low { get; set; }
            [JsonProperty("last")]
            public decimal Close { get; set; }
            [JsonProperty("bid")]
            public decimal Bid { get; set; }
            [JsonProperty("ask")]
            public decimal Ask { get; set; }
            [JsonProperty("vwap")]
            public decimal WeightedPrice { get; set; }
            [JsonProperty("volume")]
            public decimal VolumeBTC { get; set; }
            public decimal VolumeUSD { get; set; }

            /// <summary>
            /// The end time of this data. Some data covers spans (trade bars)
            /// and as such we want to know the entire time span covered
            /// </summary>
            /// <remarks>
            /// This property is overriden to allow different values for Time and EndTime
            /// if they are set in the Reader. In the base implementation EndTime equals Time
            /// </remarks>
            public override DateTime EndTime { get; set; }

            /// <summary>
            /// 1. DEFAULT CONSTRUCTOR: Custom data types need a default constructor.
            /// We search for a default constructor so please provide one here. It won't be used for data, just to generate the "Factory".
            /// </summary>
            public Bitcoin()
            {
                Symbol = "BTC";
            }

            /// <summary>
            /// 2. RETURN THE STRING URL SOURCE LOCATION FOR YOUR DATA:
            /// This is a powerful and dynamic select source file method. If you have a large dataset, 10+mb we recommend you break it into smaller files. E.g. One zip per year.
            /// We can accept raw text or ZIP files. We read the file extension to determine if it is a zip file.
            /// </summary>
            /// <param name="config">Configuration object</param>
            /// <param name="date">Date of this source file</param>
            /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
            /// <returns>String URL of source file.</returns>
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                if (isLiveMode)
                {
                    return new SubscriptionDataSource("https://www.bitstamp.net/api/ticker/", SubscriptionTransportMedium.Rest);
                }

                //return "http://my-ftp-server.com/futures-data-" + date.ToString("Ymd") + ".zip";
                // OR simply return a fixed small data file. Large files will slow down your backtest
                return new SubscriptionDataSource("https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm", SubscriptionTransportMedium.RemoteFile);
            }

            /// <summary>
            /// 3. READER METHOD: Read 1 line from data source and convert it into Object.
            /// Each line of the CSV File is presented in here. The backend downloads your file, loads it into memory and then line by line
            /// feeds it into your algorithm
            /// </summary>
            /// <param name="line">string line from the data source file submitted above</param>
            /// <param name="config">Subscription data, symbol name, data type</param>
            /// <param name="date">Current date we're requesting. This allows you to break up the data source into daily files.</param>
            /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
            /// <returns>New Bitcoin Object which extends BaseData.</returns>
            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var coin = new Bitcoin();
                if (isLiveMode)
                {
                    //Example Line Format:
                    //{"high": "441.00", "last": "421.86", "timestamp": "1411606877", "bid": "421.96", "vwap": "428.58", "volume": "14120.40683975", "low": "418.83", "ask": "421.99"}
                    try
                    {
                        coin = JsonConvert.DeserializeObject<Bitcoin>(line);
                        coin.EndTime = DateTime.UtcNow.ConvertFromUtc(config.ExchangeTimeZone);
                        coin.Value = coin.Close;
                    }
                    catch { /* Do nothing, possible error in json decoding */ }
                    return coin;
                }

                //Example Line Format:
                //Date      Open   High    Low     Close   Volume (BTC)    Volume (Currency)   Weighted Price
                //2011-09-13 5.8    6.0     5.65    5.97    58.37138238,    346.0973893944      5.929230648356
                try
                {
                    string[] data = line.Split(',');
                    coin.Time = DateTime.Parse(data[0], CultureInfo.InvariantCulture);
                    coin.EndTime = coin.Time.AddDays(1);
                    coin.Open = Convert.ToDecimal(data[1], CultureInfo.InvariantCulture);
                    coin.High = Convert.ToDecimal(data[2], CultureInfo.InvariantCulture);
                    coin.Low = Convert.ToDecimal(data[3], CultureInfo.InvariantCulture);
                    coin.Close = Convert.ToDecimal(data[4], CultureInfo.InvariantCulture);
                    coin.VolumeBTC = Convert.ToDecimal(data[5], CultureInfo.InvariantCulture);
                    coin.VolumeUSD = Convert.ToDecimal(data[6], CultureInfo.InvariantCulture);
                    coin.WeightedPrice = Convert.ToDecimal(data[7], CultureInfo.InvariantCulture);
                    coin.Value = coin.Close;
                }
                catch { /* Do nothing, skip first title row */ }

                return coin;
            }
        }
    }
}
