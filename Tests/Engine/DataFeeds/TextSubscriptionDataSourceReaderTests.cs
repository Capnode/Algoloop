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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Math.Comparers;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class TextSubscriptionDataSourceReaderTests
    {
        private SubscriptionDataConfig _config;
        private DateTime _initialDate;

        [SetUp]
        public void SetUp()
        {
            _config = new SubscriptionDataConfig(
                typeof(TestTradeBarFactory),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            _initialDate = new DateTime(2018, 1, 1);
        }

        [Test]
        public void CachedDataIsReturnedAsClone()
        {
            using var singleEntryDataCacheProvider = new SingleEntryDataCacheProvider(TestGlobals.DataProvider);
            var reader = new TextSubscriptionDataSourceReader(
                singleEntryDataCacheProvider,
                _config,
                _initialDate,
                false,
                null);
            var source = (new TradeBar()).GetSource(_config, _initialDate, false);

            var dataBars = reader.Read(source).First();
            dataBars.Value = 0;
            var dataBars2 = reader.Read(source).First();

            Assert.AreNotEqual(dataBars.Price, dataBars2.Price);
        }

        [Test]
        public void DataIsNotCachedForEphemeralDataCacheProvider()
        {
            var config = new SubscriptionDataConfig(
                    typeof(TestTradeBarFactory),
                    Symbol.Create("SymbolNonEphemeralTest1", SecurityType.Equity, Market.USA),
                    Resolution.Daily,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    false);
            using var dataCacheProvider = new CustomEphemeralDataCacheProvider { IsDataEphemeral = true};
            var reader = new TextSubscriptionDataSourceReader(
                dataCacheProvider,
                config,
                _initialDate,
                false,
                null);
            var source = (new TradeBar()).GetSource(config, _initialDate, false);
            dataCacheProvider.Data = "20000101 00:00,1,1,1,1,1";
            var dataBars = reader.Read(source).First();
            dataCacheProvider.Data = "20000101 00:00,2,2,2,2,2";
            var dataBars2 = reader.Read(source).First();

            Assert.AreEqual(new DateTime(2000, 1, 1), dataBars.Time);
            Assert.AreEqual(new DateTime(2000, 1, 1), dataBars2.Time);
            Assert.AreNotEqual(dataBars.Price, dataBars2.Price);
        }

        [Test]
        public void DataIsCachedForNonEphemeralDataCacheProvider()
        {
            var config = new SubscriptionDataConfig(
                typeof(TestTradeBarFactory),
                Symbol.Create("SymbolNonEphemeralTest2", SecurityType.Equity, Market.USA),
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            using var dataCacheProvider = new CustomEphemeralDataCacheProvider { IsDataEphemeral = false };
            var reader = new TextSubscriptionDataSourceReader(
                dataCacheProvider,
                config,
                _initialDate,
                false,
                null);
            var source = (new TradeBar()).GetSource(config, _initialDate, false);
            dataCacheProvider.Data = "20000101 00:00,1,1,1,1,1";
            var dataBars = reader.Read(source).First();
            // even if the data changes it already cached
            dataCacheProvider.Data = "20000101 00:00,2,2,2,2,2";
            var dataBars2 = reader.Read(source).First();

            Assert.AreEqual(new DateTime(2000, 1, 1), dataBars.Time);
            Assert.AreEqual(new DateTime(2000, 1, 1), dataBars2.Time);
            Assert.AreEqual(dataBars.Price, dataBars2.Price);
        }

        [Test]
        public void DataIsCachedCorrectly()
        {
            using var singleEntryDataCacheProvider = new SingleEntryDataCacheProvider(TestGlobals.DataProvider);
            var reader = new TextSubscriptionDataSourceReader(
                singleEntryDataCacheProvider,
                _config,
                _initialDate,
                false,
                null);
            var source = (new TradeBar()).GetSource(_config, _initialDate, false);

            var dataBars = reader.Read(source).ToList();
            var dataBars2 = reader.Read(source).ToList();

            Assert.AreEqual(dataBars2.Count, dataBars.Count);
            Assert.IsTrue(dataBars.SequenceEqual(dataBars2, new CustomComparer<BaseData>(
                (data, baseData) =>
                {
                    if (data.EndTime == baseData.EndTime
                        && data.Time == baseData.Time
                        && data.Symbol == baseData.Symbol
                        && data.Price == baseData.Price
                        && data.DataType == baseData.DataType
                        && data.Value == baseData.Value)
                    {
                        return 0;
                    }
                    return 1;
                })));
        }

        [Test]
        public void RespectsInitialDate()
        {
            using var singleEntryDataCacheProvider = new SingleEntryDataCacheProvider(TestGlobals.DataProvider);
            var reader = new TextSubscriptionDataSourceReader(
                singleEntryDataCacheProvider,
                _config,
                _initialDate,
                false,
                null);
            var source = (new TradeBar()).GetSource(_config, _initialDate, false);
            var dataBars = reader.Read(source).First();

            Assert.Less(dataBars.EndTime, _initialDate);

            // 80 days after _initialDate
            var initialDate2 = _initialDate.AddDays(80);
            using var defaultDataProvider2 = new DefaultDataProvider();
            using var singleEntryDataCacheProvider2 = new SingleEntryDataCacheProvider(defaultDataProvider2);
            var reader2 = new TextSubscriptionDataSourceReader(
                singleEntryDataCacheProvider2,
                _config,
                initialDate2,
                false,
                null);
            var source2 = (new TradeBar()).GetSource(_config, initialDate2, false);
            var dataBars2 = reader2.Read(source2).First();

            Assert.Less(dataBars2.EndTime, initialDate2);

            // 80 days before _initialDate
            var initialDate3 = _initialDate.AddDays(-80);
            using var defaultDataProvider3 = new DefaultDataProvider();
            using var singleEntryDataCacheProvider3 = new SingleEntryDataCacheProvider(defaultDataProvider3);
            var reader3 = new TextSubscriptionDataSourceReader(
                singleEntryDataCacheProvider3,
                _config,
                initialDate3,
                false,
                null);
            var source3 = (new TradeBar()).GetSource(_config, initialDate3, false);
            var dataBars3 = reader3.Read(source3).First();

            Assert.Less(dataBars3.EndTime, initialDate3);
        }

        [TestCase(Resolution.Daily, true)]
        [TestCase(Resolution.Hour, true)]
        [TestCase(Resolution.Minute, false)]
        [TestCase(Resolution.Second, false)]
        [TestCase(Resolution.Tick, false)]
        public void CacheBehaviorDifferentResolutions(Resolution resolution, bool shouldBeCached)
        {
            _config = new SubscriptionDataConfig(
                typeof(TestTradeBarFactory),
                Symbols.SPY,
                resolution,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            using var singleEntryDataCacheProvider = new SingleEntryDataCacheProvider(TestGlobals.DataProvider, isDataEphemeral: false);
            var reader = new TextSubscriptionDataSourceReader(
                singleEntryDataCacheProvider,
                _config,
                new DateTime(2013, 10, 07),
                false,
                null);
            var source = (new TradeBar()).GetSource(_config, new DateTime(2013, 10, 07), false);

            // first call should cache
            reader.Read(source).First();
            TestTradeBarFactory.ReaderWasCalled = false;
            reader.Read(source).First();
            Assert.AreEqual(!shouldBeCached, TestTradeBarFactory.ReaderWasCalled);
        }

        [Test, Explicit("Performance test")]
        public void CacheMissPerformance()
        {
            long counter = 0;
            var datas = new List<IEnumerable<BaseData>>();

            var factory = new TradeBar();
            using var cacheProvider = new CustomEphemeralDataCacheProvider();

            // we load SPY hour zip into memory and use it as the source of different fake tickers
            var config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            var fakeSource = factory.GetSource(config, new DateTime(2013, 10, 07), false);
            cacheProvider.Data = string.Join(Environment.NewLine, QuantConnect.Compression.ReadLines(fakeSource.Source));

            for (var i = 0; i < 500; i++)
            {
                var ticker = $"{i}";
                var fakeConfig = new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbol.Create(ticker, SecurityType.Equity, Market.USA),
                    Resolution.Hour,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    false);
                var reader = new TextSubscriptionDataSourceReader(cacheProvider, fakeConfig, Time.EndOfTime, false, null);

                var source = factory.GetSource(fakeConfig, Time.BeginningOfTime, false);
                datas.Add(reader.Read(source));
            }

            var timer = new Stopwatch();
            timer.Start();
            Parallel.ForEach(datas, enumerable =>
            {
                // after the first call should use the cache
                foreach (var data in enumerable)
                {
                    Interlocked.Increment(ref counter);
                }
            });
            timer.Stop();
            Log.Trace($"Took {timer.ElapsedMilliseconds}ms. Data count {counter}");

            timer.Reset();
            timer.Start();
            Parallel.ForEach(datas, enumerable =>
            {
                // after the first call should use the cache
                foreach (var data in enumerable)
                {
                    Interlocked.Increment(ref counter);
                }
            });
            timer.Stop();
            Log.Trace($"Took2 {timer.ElapsedMilliseconds}ms. Data count {counter}");
        }

        [Test, Explicit("Performance test")]
        public void CacheHappyPerformance()
        {
            long counter = 0;
            var datas = new List<IEnumerable<BaseData>>();

            var factory = new TradeBar();
            using var cacheProvider = new CustomEphemeralDataCacheProvider();

            // we load SPY hour zip into memory and use it as the source of different fake tickers
            var config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            var fakeSource = factory.GetSource(config, new DateTime(2013, 10, 07), false);
            cacheProvider.Data = string.Join(Environment.NewLine, QuantConnect.Compression.ReadLines(fakeSource.Source));

            for (var i = 0; i < 500; i++)
            {
                var ticker = $"{i}";
                var fakeConfig = new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbol.Create(ticker, SecurityType.Equity, Market.USA),
                    Resolution.Hour,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    false);
                var reader = new TextSubscriptionDataSourceReader(cacheProvider, fakeConfig, Time.EndOfTime, false, null);

                var source = factory.GetSource(fakeConfig, Time.BeginningOfTime, false);
                datas.Add(reader.Read(source));
            }

            var timer = new Stopwatch();
            timer.Start();
            Parallel.ForEach(datas, enumerable =>
            {
                // after the first call should use the cache
                foreach (var data in enumerable)
                {
                    Interlocked.Increment(ref counter);
                }
                foreach (var data in enumerable)
                {
                    Interlocked.Increment(ref counter);
                }
                foreach (var data in enumerable)
                {
                    Interlocked.Increment(ref counter);
                }
            });
            timer.Stop();
            Log.Trace($"Took {timer.ElapsedMilliseconds}ms. Data count {counter}");
        }

        private class TestTradeBarFactory : TradeBar
        {
            /// <summary>
            /// Will be true when data is created from a parsed file line
            /// </summary>
            public static bool ReaderWasCalled { get; set; }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                ReaderWasCalled = true;
                return base.Reader(config, line, date, isLiveMode);
            }
            public override BaseData Reader(SubscriptionDataConfig config, StreamReader streamReader, DateTime date, bool isLiveMode)
            {
                ReaderWasCalled = true;
                return base.Reader(config, streamReader, date, isLiveMode);
            }
        }

        private class CustomEphemeralDataCacheProvider : IDataCacheProvider
        {
            public string Data { set; get; }
            public bool IsDataEphemeral { set; get; }

            public List<string> GetZipEntries(string zipFile)
            {
                throw new NotImplementedException();
            }
            public Stream Fetch(string key)
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream, leaveOpen: true);
                writer.Write(Data);
                writer.Flush();
                stream.Position = 0;
                writer.Dispose();
                return stream;
            }
            public void Store(string key, byte[] data)
            {
            }
            public void Dispose()
            {
            }
        }
    }
}
