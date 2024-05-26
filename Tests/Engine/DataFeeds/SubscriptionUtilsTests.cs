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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class SubscriptionUtilsTests
    {
        private Security _security;
        private SubscriptionDataConfig _config;
        private IFactorFileProvider _factorFileProvider;

        [SetUp]
        public void SetUp()
        {
            _security = new Security(Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            _config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true, true, false);

            _factorFileProvider = TestGlobals.FactorFileProvider;
        }

        [Test]
        public void SubscriptionIsDisposed()
        {
            var dataPoints = 10;
            var enumerator = new TestDataEnumerator { MoveNextTrueCount = dataPoints };

            var subscription = SubscriptionUtils.CreateAndScheduleWorker(
                new SubscriptionRequest(
                    false,
                    null,
                    _security,
                    _config,
                    DateTime.UtcNow,
                    Time.EndOfTime
                ),
                enumerator,
                _factorFileProvider,
                false, false);

            var count = 0;
            while (enumerator.MoveNextTrueCount > 8)
            {
                if (count++ > 100)
                {
                    Assert.Fail($"Timeout waiting for producer. {enumerator.MoveNextTrueCount}");
                }
                Thread.Sleep(1);
            }

            subscription.DisposeSafely();
            Assert.IsFalse(subscription.MoveNext());
        }

        [Test]
        public void ThrowingEnumeratorStackDisposesOfSubscription()
        {
            var enumerator = new TestDataEnumerator { MoveNextTrueCount = 10, ThrowException = true};

            var subscription = SubscriptionUtils.CreateAndScheduleWorker(
                new SubscriptionRequest(
                    false,
                    null,
                    _security,
                    _config,
                    DateTime.UtcNow,
                    Time.EndOfTime
                ),
                enumerator,
                _factorFileProvider,
                false, false);

            var count = 0;
            while (enumerator.MoveNextTrueCount != 9)
            {
                if (count++ > 100)
                {
                    Assert.Fail("Timeout waiting for producer");
                }
                Thread.Sleep(1);
            }

            Assert.IsFalse(subscription.MoveNext());
            Assert.IsTrue(subscription.EndOfStream);

            // enumerator is disposed by the producer
            count = 0;
            while (!enumerator.Disposed)
            {
                if (count++ > 100)
                {
                    Assert.Fail("Timeout waiting for producer");
                }
                Thread.Sleep(1);
            }
        }

        [Test]
        // This unit tests reproduces GH 3885 where the consumer hanged forever
        public void ConsumerDoesNotHang()
        {
            for (var i = 0; i < 10000; i++)
            {
                var dataPoints = 10;

                var enumerator = new TestDataEnumerator {MoveNextTrueCount = dataPoints};

                var subscription = SubscriptionUtils.CreateAndScheduleWorker(
                    new SubscriptionRequest(
                        false,
                        null,
                        _security,
                        _config,
                        DateTime.UtcNow,
                        Time.EndOfTime
                    ),
                    enumerator,
                    _factorFileProvider,
                    false, false);

                for (var j = 0; j < dataPoints; j++)
                {
                   Assert.IsTrue(subscription.MoveNext());
                }
                Assert.IsFalse(subscription.MoveNext());
                subscription.DisposeSafely();
            }
        }

        [Test]
        public void PriceScaleFirstFillForwardBar()
        {
            var referenceTime = new DateTime(2020, 08, 06);
            var point = new Tick(referenceTime, Symbols.SPY, 1, 2);
            var point2 = point.Clone(true);
            point2.Time = referenceTime;
            var point3 = point.Clone(false);
            point3.Time = referenceTime.AddDays(1);
            ;
            var enumerator = new List<BaseData> { point2, point3 }.GetEnumerator();
            var factorFileProfider = new Mock<IFactorFileProvider>();

            var factorFile = new CorporateFactorProvider(_security.Symbol.Value, new[]
            {
                new CorporateFactorRow(referenceTime, 0.5m, 1),
                new CorporateFactorRow(referenceTime.AddDays(1), 1m, 1)
            }, referenceTime);

            factorFileProfider.Setup(s => s.Get(It.IsAny<Symbol>())).Returns(factorFile);

            var subscription = SubscriptionUtils.CreateAndScheduleWorker(
                new SubscriptionRequest(
                    false,
                    null,
                    _security,
                    _config,
                    referenceTime,
                    Time.EndOfTime
                ),
                enumerator,
                factorFileProfider.Object,
                true, false);

            Assert.IsTrue(subscription.MoveNext());
            // we do expect it to pick up the prev factor file scale
            Assert.AreEqual(1, (subscription.Current.Data as Tick).AskPrice);
            Assert.IsTrue((subscription.Current.Data as Tick).IsFillForward);

            Assert.IsTrue(subscription.MoveNext());
            Assert.AreEqual(2, (subscription.Current.Data as Tick).AskPrice);
            Assert.IsFalse((subscription.Current.Data as Tick).IsFillForward);

            subscription.DisposeSafely();
        }

        [Test]
        public void PriceScaleDoesNotUpdateForFillForwardBar()
        {
            var referenceTime = new DateTime(2020, 08, 06);
            var point = new Tick(referenceTime, Symbols.SPY, 1, 2);
            var point2 = point.Clone(true);
            point2.Time = referenceTime.AddDays(1);
            var point3 = point.Clone(false);
            point3.Time = referenceTime.AddDays(2);
            ;
            var enumerator = new List<BaseData> { point, point2, point3 }.GetEnumerator();
            var factorFileProfider = new Mock<IFactorFileProvider>();

            var factorFile = new CorporateFactorProvider(_security.Symbol.Value, new[]
            {
                new CorporateFactorRow(referenceTime, 0.5m, 1),
                new CorporateFactorRow(referenceTime.AddDays(1), 1m, 1)
            }, referenceTime);

            factorFileProfider.Setup(s => s.Get(It.IsAny<Symbol>())).Returns(factorFile);

            var subscription = SubscriptionUtils.CreateAndScheduleWorker(
                new SubscriptionRequest(
                    false,
                    null,
                    _security,
                    _config,
                    referenceTime,
                    Time.EndOfTime
                ),
                enumerator,
                factorFileProfider.Object,
                true, false);

            Assert.IsTrue(subscription.MoveNext());
            Assert.AreEqual(1, (subscription.Current.Data as Tick).AskPrice);
            Assert.IsFalse((subscription.Current.Data as Tick).IsFillForward);

            Assert.IsTrue(subscription.MoveNext());
            Assert.AreEqual(1, (subscription.Current.Data as Tick).AskPrice);
            Assert.IsTrue((subscription.Current.Data as Tick).IsFillForward);

            Assert.IsTrue(subscription.MoveNext());
            Assert.AreEqual(2, (subscription.Current.Data as Tick).AskPrice);
            Assert.IsFalse((subscription.Current.Data as Tick).IsFillForward);

            subscription.DisposeSafely();
        }

        [TestCase(typeof(TradeBar), true)]
        [TestCase(typeof(OpenInterest), false)]
        [TestCase(typeof(QuoteBar), false)]
        public void SubscriptionEmitsAuxData(Type typeOfConfig, bool shouldReceiveAuxData)
        {
            var config = new SubscriptionDataConfig(typeOfConfig, _security.Symbol, Resolution.Hour, TimeZones.NewYork, TimeZones.NewYork, true, true, false);

            var totalPoints = 8;
            var time = new DateTime(2010, 1, 1);
            var enumerator = Enumerable.Range(0, totalPoints).Select(x => new Delisting { Time = time.AddHours(x) }).GetEnumerator();
            var subscription = SubscriptionUtils.CreateAndScheduleWorker(
                new SubscriptionRequest(
                    false,
                    null,
                    _security,
                    config,
                    DateTime.UtcNow,
                    Time.EndOfTime
                ),
                enumerator,
                _factorFileProvider,
                false, false);

            // Test our subscription stream to see if it emits the aux data it should be filtered
            // by the SubscriptionUtils produce function if the config isn't for a TradeBar
            int dataReceivedCount = 0;
            while (subscription.MoveNext())
            {
                dataReceivedCount++;
                if (subscription.Current != null && subscription.Current.Data.DataType == MarketDataType.Auxiliary)
                {
                    Assert.IsTrue(shouldReceiveAuxData);
                }
            }

            // If it should receive aux data it should have emitted all points
            // otherwise none should have been emitted
            if (shouldReceiveAuxData)
            {
                Assert.AreEqual(totalPoints, dataReceivedCount);
            }
            else
            {
                Assert.AreEqual(0, dataReceivedCount);
            }
        }

        private class TestDataEnumerator : IEnumerator<BaseData>
        {
            public bool ThrowException { get; set; }
            public bool Disposed { get; set; }
            public int MoveNextTrueCount { get; set; }

            public void Dispose()
            {
                Disposed = true;
            }

            public bool MoveNext()
            {
                Current = new Tick(DateTime.UtcNow,Symbols.SPY, 1, 2);
                var result = --MoveNextTrueCount >= 0;
                if (ThrowException)
                {
                    throw new Exception("TestDataEnumerator.MoveNext()");
                }
                return result;
            }

            public void Reset()
            {
            }

            public BaseData Current { get; set; }

            object IEnumerator.Current => Current;
        }
    }
}
