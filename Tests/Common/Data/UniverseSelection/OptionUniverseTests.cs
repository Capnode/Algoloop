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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuantConnect.Tests.Common.Securities.Options
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class OptionUniverseTests
    {
        private static string TestOptionUniverseFile = @"
#expiry,strike,right,open,high,low,close,volume,open_interest,implied_volatility,delta,gamma,vega,theta,rho
,,,5488.47998046875,5523.64013671875,5451.1201171875,5460.47998046875,7199220000,,,,,,,
20260618,5400,C,780.3000,853.9000,709.6000,767.7500,0,135,0.1637928,0.6382026,0.0002890,26.5721377,-0.5042690,55.5035521
20261218,5400,C,893.1400,907.7100,893.1400,907.5400,37,1039,0.1701839,0.6420671,0.0002447,28.9774913,-0.4608812,67.5259867
20271217,5400,C,1073.0000,1073.0000,1073.0000,1073.0000,0,889,0.1839256,0.6456981,0.0001858,32.6109403,-0.3963479,88.5870185
20281215,5400,C,1248.0000,1248.0000,1248.0000,1248.0000,0,301,0.1934730,0.6472619,0.0001512,35.1083627,-0.3434647,106.9858230
20291221,5400,C,1467.9000,1467.9000,1467.9000,1467.9000,0,9,0.2046702,0.6460372,0.0001254,36.9157598,-0.2993105,122.2236355
20240719,5405,C,95.4500,95.4500,95.4500,95.4500,1,311,0.1006795,0.6960459,0.0026897,4.4991247,-1.4284818,2.0701880
20240816,5405,C,161.4000,161.4000,161.4000,161.4000,0,380,0.1088739,0.6472976,0.0017128,7.3449930,-1.1139626,4.5112640
20240920,5405,C,213.7000,213.7000,211.0000,211.0000,0,33,0.1149306,0.6316343,0.0012532,9.7567496,-0.9462173,7.4872272
20241018,5405,C,254.0000,303.3500,218.2500,238.0500,0,0,0.1183992,0.6273390,0.0010556,11.2892617,-0.8673778,9.8420483
20240719,5410,C,143.5900,143.5900,119.7100,119.7100,11,355,0.0995106,0.6842402,0.0027673,4.5750811,-1.4291241,2.0364155
20240816,5410,C,151.2000,151.2000,151.2000,151.2000,0,68,0.1080883,0.6395066,0.0017388,7.4027436,-1.1113164,4.4598077
20240920,5410,C,202.5000,202.5000,201.9800,201.9800,0,211,0.1142983,0.6258911,0.0012667,9.8073284,-0.9438102,7.4239078
20241018,5410,C,256.4800,256.4800,255.9000,255.9000,0,91,0.1180060,0.6223570,0.0010637,11.3388534,-0.8661655,9.7694707
20241115,5410,C,279.7500,279.7500,279.2300,279.2300,0,65,0.1268034,0.6170056,0.0008881,12.7072390,-0.8357895,11.9829003
20240719,5415,C,123.1800,123.1800,98.0300,98.0300,5,307,0.0985516,0.6716430,0.0028403,4.6505424,-1.4312099,2.0001484
20240816,5415,C,146.6900,146.6900,146.6900,146.6900,3,901,0.1073207,0.6315307,0.0017645,7.4585091,-1.1084001,4.4069495
20240920,5415,C,194.1000,196.7000,194.1000,196.7000,0,63,0.1136398,0.6200837,0.0012804,9.8561442,-0.9410592,7.3597879
20241018,5415,C,246.5000,295.7500,210.7500,230.9500,0,0,0.1172852,0.6175838,0.0010746,11.3844988,-0.8632046,9.7014393
20240719,5420,C,119.7500,119.7500,94.0000,94.0000,31,453,0.0973479,0.6589639,0.0029188,4.7207612,-1.4288180,1.9636645
20240816,5420,C,181.5800,181.5800,154.8300,154.8300,4,110,0.1065704,0.6233721,0.0017897,7.5120648,-1.1051922,4.3527055
".TrimStart();

        private List<OptionUniverse> _optionUniverseFile;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var config = new SubscriptionDataConfig(typeof(OptionUniverse),
                Symbol.CreateCanonicalOption(Symbols.SPX),
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            var date = new DateTime(2024, 06, 28);

            _optionUniverseFile = new List<OptionUniverse>();
            var factory = new OptionUniverse();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(TestOptionUniverseFile));
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var data = (OptionUniverse)factory.Reader(config, reader, date, false);
                if (data == null) continue;
                _optionUniverseFile.Add(data);
            }
        }

        [Test]
        public void RoundTripCsvConversion()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("#" + OptionUniverse.CsvHeader(SecurityType.Option));

            foreach (var data in _optionUniverseFile)
            {
                string csv = null;
                if (data.Symbol.SecurityType.IsOption())
                {
                    csv = OptionUniverse.ToCsv(data.Symbol, data.Open, data.High, data.Low, data.Close, data.Volume, data.OpenInterest,
                        data.ImpliedVolatility, data.Greeks);
                }
                else
                {
                    csv = OptionUniverse.ToCsv(data.Symbol, data.Open, data.High, data.Low, data.Close, data.Volume, null, null, null);
                }

                stringBuilder.AppendLine(csv);
            }

            var csvString = stringBuilder.ToString();
            Assert.AreEqual(TestOptionUniverseFile, csvString);
        }
    }
}
