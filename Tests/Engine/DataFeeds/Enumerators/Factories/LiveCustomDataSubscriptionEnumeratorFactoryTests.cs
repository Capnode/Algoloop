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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators.Factories
{
    [TestFixture]
    public class LiveCustomDataSubscriptionEnumeratorFactoryTests
    {
        [TestFixture]
        public class WhenCreatingEnumeratorForRestData
        {
            private readonly DateTime _referenceLocal = new DateTime(2017, 10, 12);
            private readonly DateTime _referenceUtc = new DateTime(2017, 10, 12).ConvertToUtc(TimeZones.NewYork);

            private ManualTimeProvider _timeProvider;
            private IEnumerator<BaseData> _enumerator;
            private Mock<ISubscriptionDataSourceReader> _dataSourceReader;

            [SetUp]
            public void Given()
            {
                _timeProvider = new ManualTimeProvider(_referenceUtc);

                _dataSourceReader = new Mock<ISubscriptionDataSourceReader>();
                _dataSourceReader.Setup(dsr => dsr.Read(It.Is<SubscriptionDataSource>(sds =>
                        sds.Source == "rest.source" &&
                        sds.TransportMedium == SubscriptionTransportMedium.Rest &&
                        sds.Format == FileFormat.Csv))
                    )
                    .Returns(Enumerable.Range(0, 100)
                        .Select(i => new RestData
                        {
                            EndTime = _referenceLocal.AddSeconds(i)
                        }))
                        .Verifiable();

                var config = new SubscriptionDataConfig(typeof(RestData), Symbols.SPY, Resolution.Second, TimeZones.NewYork, TimeZones.NewYork, false, false, false);
                var request = GetSubscriptionRequest(config, _referenceUtc.AddSeconds(-1), _referenceUtc.AddDays(1));

                var factory = new TestableLiveCustomDataSubscriptionEnumeratorFactory(_timeProvider, _dataSourceReader.Object);
                _enumerator = factory.CreateEnumerator(request, null);
            }

            [TearDown]
            public void TearDown()
            {
                _enumerator?.DisposeSafely();
            }

            [Test]
            public void YieldsDataEachSecondAsTimePasses()
            {
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNotNull(_enumerator.Current);
                Assert.AreEqual(_referenceLocal, _enumerator.Current.EndTime);

                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);

                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);

                _timeProvider.AdvanceSeconds(1);

                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNotNull(_enumerator.Current);
                Assert.AreEqual(_referenceLocal.AddSeconds(1), _enumerator.Current.EndTime);

                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);

                VerifyGetSourceInvocationCount(_dataSourceReader, 1, "rest.source", SubscriptionTransportMedium.Rest, FileFormat.Csv);
            }
        }

        [TestFixture]
        public class WhenCreatingEnumeratorForRestCollectionData
        {
            private const int DataPerTimeStep = 3;
            private readonly DateTime _referenceLocal = new DateTime(2017, 10, 12);
            private readonly DateTime _referenceUtc = new DateTime(2017, 10, 12).ConvertToUtc(TimeZones.NewYork);

            private ManualTimeProvider _timeProvider;
            private IEnumerator<BaseData> _enumerator;
            private Mock<ISubscriptionDataSourceReader> _dataSourceReader;

            [SetUp]
            public void Given()
            {
                _timeProvider = new ManualTimeProvider(_referenceUtc);

                _dataSourceReader = new Mock<ISubscriptionDataSourceReader>();
                _dataSourceReader.Setup(dsr => dsr.Read(It.Is<SubscriptionDataSource>(sds =>
                        sds.Source == "rest.collection.source" &&
                        sds.TransportMedium == SubscriptionTransportMedium.Rest &&
                        sds.Format == FileFormat.UnfoldingCollection))
                    )
                    .Returns(Enumerable.Range(0, 100)
                        .Select(i => new BaseDataCollection(_referenceLocal.AddSeconds(i), Symbols.SPY, Enumerable.Range(0, DataPerTimeStep)
                            .Select(_ => new RestCollectionData {EndTime = _referenceLocal.AddSeconds(i)})))
                    )
                    .Verifiable();

                var config = new SubscriptionDataConfig(typeof(RestCollectionData), Symbols.SPY, Resolution.Second, TimeZones.NewYork, TimeZones.NewYork, false, false, false);
                var request = GetSubscriptionRequest(config, _referenceUtc.AddSeconds(-4), _referenceUtc.AddDays(1));

                var factory = new TestableLiveCustomDataSubscriptionEnumeratorFactory(_timeProvider, _dataSourceReader.Object);
                _enumerator = factory.CreateEnumerator(request, null);
            }

            [TearDown]
            public void TearDown()
            {
                _enumerator?.DisposeSafely();
            }

            [Test]
            public void YieldsGroupOfDataEachSecond()
            {
                for (int i = 0; i < DataPerTimeStep; i++)
                {
                    Assert.IsTrue(_enumerator.MoveNext());
                    Assert.IsNotNull(_enumerator.Current, $"Index {i} is null.");
                    Assert.AreEqual(_referenceLocal, _enumerator.Current.EndTime);
                }

                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);

                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);

                _timeProvider.AdvanceSeconds(1);

                for (int i = 0; i < DataPerTimeStep; i++)
                {
                    Assert.IsTrue(_enumerator.MoveNext());
                    Assert.IsNotNull(_enumerator.Current);
                    Assert.AreEqual(_referenceLocal.AddSeconds(1), _enumerator.Current.EndTime);
                }

                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);

                VerifyGetSourceInvocationCount(_dataSourceReader, 1, "rest.collection.source", SubscriptionTransportMedium.Rest, FileFormat.UnfoldingCollection);
            }
        }

        [TestFixture]
        public class WhenCreatingEnumeratorForRemoteCollectionData
        {
            private const int DataPerTimeStep = 3;
            private readonly DateTime _referenceLocal = new DateTime(2017, 10, 12);
            private readonly DateTime _referenceUtc = new DateTime(2017, 10, 12).ConvertToUtc(TimeZones.NewYork);

            private ManualTimeProvider _timeProvider;
            private IEnumerator<BaseData> _enumerator;

            [SetUp]
            public void Given()
            {
                _timeProvider = new ManualTimeProvider(_referenceUtc);
                var dataSourceReader = new TestISubscriptionDataSourceReader
                {
                    TimeProvider = _timeProvider
                };

                var config = new SubscriptionDataConfig(typeof(RemoteCollectionData), Symbols.SPY, Resolution.Second, TimeZones.NewYork, TimeZones.NewYork, false, false, false);
                var request = GetSubscriptionRequest(config, _referenceUtc.AddSeconds(-4), _referenceUtc.AddDays(1));

                var factory = new TestableLiveCustomDataSubscriptionEnumeratorFactory(_timeProvider, dataSourceReader);
                _enumerator = factory.CreateEnumerator(request, null);
            }

            private class TestISubscriptionDataSourceReader : ISubscriptionDataSourceReader
            {
                public ManualTimeProvider TimeProvider;
                public event EventHandler<InvalidSourceEventArgs> InvalidSource;

                public IEnumerable<BaseData> Read(SubscriptionDataSource source)
                {
                    var currentLocalTime = TimeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork);
                    var data = Enumerable.Range(0, DataPerTimeStep).Select(_ => new RemoteCollectionData { EndTime = currentLocalTime });

                    // let's add some old data which should be ignored
                    data = data.Concat(Enumerable.Range(0, DataPerTimeStep).Select(_ => new RemoteCollectionData { EndTime = currentLocalTime.AddSeconds(-1) }));
                    return new BaseDataCollection(currentLocalTime, Symbols.SPY, data);
                }
            }

            [TearDown]
            public void TearDown()
            {
                _enumerator?.DisposeSafely();
            }

            [Test]
            public void YieldsGroupOfDataEachSecond()
            {
                for (int i = 0; i < DataPerTimeStep; i++)
                {
                    Assert.IsTrue(_enumerator.MoveNext());
                    Assert.IsNotNull(_enumerator.Current, $"Index {i} is null.");
                    Assert.AreEqual(_referenceLocal, _enumerator.Current.EndTime);
                }

                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);

                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);

                _timeProvider.AdvanceSeconds(1);

                for (int i = 0; i < DataPerTimeStep; i++)
                {
                    Assert.IsTrue(_enumerator.MoveNext());
                    Assert.IsNotNull(_enumerator.Current);
                    Assert.AreEqual(_referenceLocal.AddSeconds(1), _enumerator.Current.EndTime);
                }

                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);
            }
        }

        [TestFixture]
        public class WhenCreatingEnumeratorForSecondRemoteFileData
        {
            private readonly DateTime _referenceLocal = new DateTime(2017, 10, 12);
            private readonly DateTime _referenceUtc = new DateTime(2017, 10, 12).ConvertToUtc(TimeZones.NewYork);

            private ManualTimeProvider _timeProvider;
            private IEnumerator<BaseData> _enumerator;
            private Mock<ISubscriptionDataSourceReader> _dataSourceReader;

            [SetUp]
            public void Given()
            {
                _timeProvider = new ManualTimeProvider(_referenceUtc);

                _dataSourceReader = new Mock<ISubscriptionDataSourceReader>();
                _dataSourceReader.Setup(dsr => dsr.Read(It.Is<SubscriptionDataSource>(sds =>
                        sds.Source == "remote.file.source" &&
                        sds.TransportMedium == SubscriptionTransportMedium.RemoteFile &&
                        sds.Format == FileFormat.Csv))
                    )
                    .Returns(Enumerable.Range(0, 100)
                        .Select(i => new RemoteFileData
                        {
                            // include past data
                            EndTime = _referenceLocal.AddSeconds(i - 95)
                        }))
                    .Verifiable();

                var config = new SubscriptionDataConfig(typeof(RemoteFileData), Symbols.SPY, Resolution.Second, TimeZones.NewYork, TimeZones.NewYork, false, false, false);
                var request = GetSubscriptionRequest(config, _referenceUtc.AddSeconds(-6), _referenceUtc.AddDays(1));

                var factory = new TestableLiveCustomDataSubscriptionEnumeratorFactory(_timeProvider, _dataSourceReader.Object);
                _enumerator = factory.CreateEnumerator(request, null);
            }

            [TearDown]
            public void TearDown()
            {
                _enumerator?.DisposeSafely();
            }

            [Test]
            public void YieldsDataEachSecondAsTimePasses()
            {
                // most recent 5 seconds of data
                for (int i = 5; i > 0; i--)
                {
                    Assert.IsTrue(_enumerator.MoveNext());
                    Assert.IsNotNull(_enumerator.Current);
                    Assert.AreEqual(_referenceLocal.AddSeconds(-i), _enumerator.Current.EndTime);
                }

                // first data point
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNotNull(_enumerator.Current);
                Assert.AreEqual(_referenceLocal, _enumerator.Current.EndTime);

                _timeProvider.AdvanceSeconds(1);

                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNotNull(_enumerator.Current);
                Assert.AreEqual(_referenceLocal.AddSeconds(1), _enumerator.Current.EndTime);

                VerifyGetSourceInvocationCount(_dataSourceReader, 1, "remote.file.source", SubscriptionTransportMedium.RemoteFile, FileFormat.Csv);
            }
        }

        [TestFixture]
        public class WhenCreatingEnumeratorForDailyRemoteFileData
        {
            private int _dataPointsAfterReference = 1;
            private readonly DateTime _referenceLocal = new DateTime(2017, 10, 12);
            private readonly DateTime _referenceUtc = new DateTime(2017, 10, 12).ConvertToUtc(TimeZones.NewYork);

            private ManualTimeProvider _timeProvider;
            private IEnumerator<BaseData> _enumerator;
            private Mock<ISubscriptionDataSourceReader> _dataSourceReader;

            [SetUp]
            public void Given()
            {
                _timeProvider = new ManualTimeProvider(_referenceUtc);

                _dataSourceReader = new Mock<ISubscriptionDataSourceReader>();
                _dataSourceReader.Setup(dsr => dsr.Read(It.Is<SubscriptionDataSource>(sds =>
                        sds.Source == "remote.file.source" &&
                        sds.TransportMedium == SubscriptionTransportMedium.RemoteFile &&
                        sds.Format == FileFormat.Csv))
                    )
                    .Returns(() => Enumerable.Range(0, 100)
                        .Select(i => new RemoteFileData
                        {
                            // include past data
                            EndTime = _referenceLocal.Add(TimeSpan.FromDays(i - (100 - _dataPointsAfterReference - 1)))
                        }))
                    .Verifiable();

                var config = new SubscriptionDataConfig(typeof(RemoteFileData), Symbols.SPY, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false);
                var request = GetSubscriptionRequest(config, _referenceUtc.AddDays(-2), _referenceUtc.AddDays(1));

                var factory = new TestableLiveCustomDataSubscriptionEnumeratorFactory(_timeProvider, _dataSourceReader.Object);
                _enumerator = factory.CreateEnumerator(request, null);
            }

            [TearDown]
            public void TearDown()
            {
                _enumerator?.DisposeSafely();
            }

            [Test]
            public void YieldsDataEachDayAsTimePasses()
            {
                // previous point is exactly one resolution step behind, so it emits
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNotNull(_enumerator.Current);
                Assert.AreEqual(_referenceLocal.AddDays(-1), _enumerator.Current.EndTime);
                VerifyGetSourceInvocation(1);

                // yields the data for the current time
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNotNull(_enumerator.Current);
                Assert.AreEqual(_referenceLocal, _enumerator.Current.EndTime);
                VerifyGetSourceInvocation(0);

                _timeProvider.Advance(Time.OneDay);

                // now we can yield the next data point as it has passed frontier time
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNotNull(_enumerator.Current);
                Assert.AreEqual(_referenceLocal.AddDays(1), _enumerator.Current.EndTime);
                VerifyGetSourceInvocation(0);

                // this call exhaused the enumerator stack and yields a null result
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);
                VerifyGetSourceInvocation(0);

                // this call refrshes the enumerator stack but finds no data ahead of the frontier
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);
                VerifyGetSourceInvocation(1);

                _timeProvider.Advance(TimeSpan.FromMinutes(30));

                // time advances 30 minutes so we'll try to refresh again
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);
                VerifyGetSourceInvocation(1);

                _timeProvider.Advance(Time.OneDay);

                // now to the next day, we'll try again and get data
                _dataPointsAfterReference++;
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNotNull(_enumerator.Current);
                Assert.AreEqual(_referenceLocal.AddDays(2), _enumerator.Current.EndTime);
                VerifyGetSourceInvocation(1);

                _timeProvider.Advance(TimeSpan.FromHours(1));

                // out of data
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);
                VerifyGetSourceInvocation(0);

                _timeProvider.Advance(TimeSpan.FromHours(1));

                // time advanced so we'll try to refresh the souce again, but exhaust the stack because no data
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);
                VerifyGetSourceInvocation(1);

                // move forward to next whole day, midnight
                _timeProvider.Advance(Time.OneDay.Subtract(TimeSpan.FromHours(2.5)));

                // the day elapsed but there's still no data available
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);
                VerifyGetSourceInvocation(1);

                // this is rate limited by the 30 minute guard for daily data
                _timeProvider.Advance(TimeSpan.FromMinutes(29));
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);
                VerifyGetSourceInvocation(0);

                // another 30 minutes elapsed and now there's data available
                _dataPointsAfterReference++;
                _timeProvider.Advance(TimeSpan.FromMinutes(1));

                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNotNull(_enumerator.Current);
                Assert.AreEqual(_referenceLocal.AddDays(3), _enumerator.Current.EndTime);
                VerifyGetSourceInvocation(1);

                // exhausted the stack
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);
                VerifyGetSourceInvocation(0);

                // rate limited
                Assert.IsTrue(_enumerator.MoveNext());
                Assert.IsNull(_enumerator.Current);
                VerifyGetSourceInvocation(0);
            }

            private int _runningCount;
            private void VerifyGetSourceInvocation(int count)
            {
                _runningCount += count;
                VerifyGetSourceInvocationCount(_dataSourceReader, _runningCount, "remote.file.source", SubscriptionTransportMedium.RemoteFile, FileFormat.Csv);
            }
        }

        [TestCase(10)]
        [TestCase(60)]
        [TestCase(0)]
        public void AllowsSpecifyingIntervalCheck(int intervalCheck)
        {
            var referenceLocal = new DateTime(2017, 10, 12);
            var referenceUtc = new DateTime(2017, 10, 12).ConvertToUtc(TimeZones.NewYork);

            var timeProvider = new ManualTimeProvider(referenceUtc);

            var callCount = 0;
            var dataSourceReader = new Mock<ISubscriptionDataSourceReader>();
            dataSourceReader.Setup(dsr => dsr.Read(It.Is<SubscriptionDataSource>(sds =>
                    sds.Source == "local.file.source" &&
                    sds.TransportMedium == SubscriptionTransportMedium.LocalFile &&
                    sds.Format == FileFormat.Csv))
                )
                .Returns(() => new []{ new LocalFileData { EndTime = referenceLocal.AddSeconds(++callCount) } })
                .Verifiable();

            var config = new SubscriptionDataConfig(typeof(LocalFileData), Symbols.SPY, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false);
            var request = GetSubscriptionRequest(config, referenceUtc.AddSeconds(-1), referenceUtc.AddDays(1));

            var intervalCalls = intervalCheck == 0 ? (TimeSpan?) null : TimeSpan.FromMinutes(intervalCheck);

            var factory = new TestableLiveCustomDataSubscriptionEnumeratorFactory(timeProvider, dataSourceReader.Object, intervalCalls);
            var enumerator = factory.CreateEnumerator(request, null);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            Assert.AreEqual(referenceLocal.AddSeconds(callCount), enumerator.Current.EndTime);

            VerifyGetSourceInvocationCount(dataSourceReader, 1, "local.file.source", SubscriptionTransportMedium.LocalFile, FileFormat.Csv);

            // time didn't pass so should refresh
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
            VerifyGetSourceInvocationCount(dataSourceReader, 1, "local.file.source", SubscriptionTransportMedium.LocalFile, FileFormat.Csv);

            var expectedInterval = intervalCalls ?? TimeSpan.FromMinutes(30);

            timeProvider.Advance(expectedInterval.Add(-TimeSpan.FromSeconds(2)));
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
            VerifyGetSourceInvocationCount(dataSourceReader, 1, "local.file.source", SubscriptionTransportMedium.LocalFile, FileFormat.Csv);

            timeProvider.Advance(TimeSpan.FromSeconds(2));
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            Assert.AreEqual(referenceLocal.AddSeconds(callCount), enumerator.Current.EndTime);
            VerifyGetSourceInvocationCount(dataSourceReader, 2, "local.file.source", SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        private static void VerifyGetSourceInvocationCount(Mock<ISubscriptionDataSourceReader> dataSourceReader, int count, string source, SubscriptionTransportMedium medium, FileFormat fileFormat)
        {
            dataSourceReader.Verify(dsr => dsr.Read(It.Is<SubscriptionDataSource>(sds =>
                sds.Source == source && sds.TransportMedium == medium && sds.Format == fileFormat)), Times.Exactly(count));
        }

        private static SubscriptionRequest GetSubscriptionRequest(SubscriptionDataConfig config, DateTime startTime, DateTime endTime)
        {
            var quoteCurrency = new Cash(Currencies.USD, 0, 1);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, Symbols.SPY, SecurityType.Equity);
            var security = new Equity(
                Symbols.SPY,
                exchangeHours,
                quoteCurrency,
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            return new SubscriptionRequest(false, null, security, config, startTime, endTime);
        }


        class RestData : BaseData
        {
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource("rest.source", SubscriptionTransportMedium.Rest);
            }
        }

        class RemoteCollectionData : BaseData
        {
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource("remote.collection.source", SubscriptionTransportMedium.RemoteFile, FileFormat.UnfoldingCollection);
            }
        }

        class RestCollectionData : BaseData
        {
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource("rest.collection.source", SubscriptionTransportMedium.Rest, FileFormat.UnfoldingCollection);
            }
        }

        class RemoteFileData : BaseData
        {
            public override DateTime EndTime
            {
                get { return Time + QuantConnect.Time.OneDay; }
                set { Time = value - QuantConnect.Time.OneDay; }
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource("remote.file.source", SubscriptionTransportMedium.RemoteFile);
            }
        }

        class LocalFileData : BaseData
        {
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource("local.file.source", SubscriptionTransportMedium.LocalFile);
            }
        }

        class TestableLiveCustomDataSubscriptionEnumeratorFactory : LiveCustomDataSubscriptionEnumeratorFactory
        {
            private readonly ISubscriptionDataSourceReader _dataSourceReader;

            public TestableLiveCustomDataSubscriptionEnumeratorFactory(ITimeProvider timeProvider, ISubscriptionDataSourceReader dataSourceReader, TimeSpan? minimumIntervalCheck = null)
                : base(timeProvider, null, minimumIntervalCheck: minimumIntervalCheck)
            {
                _dataSourceReader = dataSourceReader;
            }

            protected override ISubscriptionDataSourceReader GetSubscriptionDataSourceReader(SubscriptionDataSource source,
                IDataCacheProvider dataCacheProvider,
                SubscriptionDataConfig config,
                DateTime date,
                BaseData baseData,
                IDataProvider dataProvider)
            {
                return _dataSourceReader;
            }
        }
    }
}
