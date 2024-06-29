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
using System.IO;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Data.Auxiliary
{
    [TestFixture]
    public class LocalZipMapFileProviderTests
    {
        private string _zipFilePath;

        [OneTimeSetUp]
        public void Setup()
        {
            // Take our repo included map files and zip them up for these tests
            var date = DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork).Date.AddDays(-1);
            var path = Path.Combine(Globals.DataFolder, $"equity/usa/map_files/");
            var tmp = "./tmp.zip";

            _zipFilePath = Path.Combine(Globals.DataFolder, $"equity/usa/map_files/map_files_{date:yyyyMMdd}.zip");

            // Have to compress to tmp file or else it doesn't finish reading all the files in dir
            QuantConnect.Compression.ZipDirectory(path, tmp);
            File.Move(tmp, _zipFilePath, true);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (File.Exists(_zipFilePath))
            {
                File.Delete(_zipFilePath);
            }
        }

        [Test]
        public void Retrieves()
        {
            var fileProviderTest = new LocalZipMapFileProviderTest();
            using var dataProviderTest = new DefaultDataProviderTest();
            fileProviderTest.Initialize(dataProviderTest);

            var mapFileResolver = fileProviderTest.Get(AuxiliaryDataKey.EquityUsa);

            fileProviderTest.Enabled = false;
            dataProviderTest.DisposeSafely();

            Assert.IsNotEmpty(mapFileResolver);
        }

        [Test]
        public void CacheIsCleared()
        {
            var fileProviderTest = new LocalZipMapFileProviderTest();
            using var dataProviderTest = new DefaultDataProviderTest();
            fileProviderTest.Initialize(dataProviderTest);
            fileProviderTest.CacheCleared.Reset();

            fileProviderTest.Get(AuxiliaryDataKey.EquityUsa);
            Assert.AreEqual(1, dataProviderTest.FetchCount);
            Thread.Sleep(50);
            fileProviderTest.Get(AuxiliaryDataKey.EquityUsa);
            Assert.AreEqual(1, dataProviderTest.FetchCount);

            fileProviderTest.CacheCleared.WaitOne(TimeSpan.FromSeconds(2));
            fileProviderTest.Get(AuxiliaryDataKey.EquityUsa);
            Assert.AreEqual(2, dataProviderTest.FetchCount);

            fileProviderTest.Enabled = false;
            dataProviderTest.DisposeSafely();
        }

        private class LocalZipMapFileProviderTest : LocalZipMapFileProvider
        {
            public bool Enabled = true;
            public readonly ManualResetEvent CacheCleared = new(false);
            protected override TimeSpan CacheRefreshPeriod => TimeSpan.FromMilliseconds(300);

            protected override void StartExpirationTask()
            {
                if (Enabled)
                {
                    base.StartExpirationTask();
                    CacheCleared.Set();
                }
            }
        }

        private class DefaultDataProviderTest : DefaultDataProvider
        {
            public int FetchCount { get; set; }

            public override Stream Fetch(string key)
            {
                FetchCount++;
                return base.Fetch(key);
            }
        }
    }
}
