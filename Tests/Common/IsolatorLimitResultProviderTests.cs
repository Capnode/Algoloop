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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Scheduling;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class IsolatorLimitResultProviderTests
    {
        private TimeMonitorTest _timeMonitor;
        private AutoResetEvent _timeMonitorEvent;

        [OneTimeSetUp]
        public void Setup()
        {
            _timeMonitor = new TimeMonitorTest(monitorIntervalMs:3);
            _timeMonitorEvent = new AutoResetEvent(false);
            _timeMonitor.TimeMonitorEvent = _timeMonitorEvent;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _timeMonitor.DisposeSafely();
        }

        [Test]
        public void ConsumeRequestsAdditionalTimeAfterOneMinute()
        {
            using var minuteElapsed = new ManualResetEvent(false);
            using var consumeStarted = new ManualResetEvent(false);

            Action code = () =>
            {
                if (!minuteElapsed.WaitOne(10000))
                {
                    throw new TimeoutException("minuteElapsed");
                }
            };
            var provider = new FakeIsolatorLimitResultProvider();
            var timeProvider = new ManualTimeProvider(new DateTime(2000, 01, 01));

            var consumeCompleted = Task.Run(() =>
            {
                consumeStarted.Set();
                provider.Consume(timeProvider, code, _timeMonitor);
            });

            if (!consumeStarted.WaitOne(50))
            {
                Assert.Fail("Consume should have started.");
            }

            // Let's give the monitor time to register the initial time
            _timeMonitorEvent.WaitOne();
            timeProvider.Advance(TimeSpan.FromSeconds(45));

            Assert.AreEqual(0, provider.Invocations.Count);

            timeProvider.Advance(TimeSpan.FromSeconds(15));

            _timeMonitorEvent.WaitOne();

            minuteElapsed.Set();
            if (!consumeCompleted.Wait(50))
            {
                Assert.Fail("Consume should have returned.");
            }

            Assert.AreEqual(1, provider.Invocations.Count);
            Assert.AreEqual(1, provider.Invocations[0]);

            // give time to the monitor to register the time consumer ended
            _timeMonitorEvent.WaitOne();
            Assert.AreEqual(0, _timeMonitor.Count);
        }

        [Test]
        public void ConsumeDoesNotRequestAdditionalTimeBeforeOneMinute()
        {
            var timeProvider = new ManualTimeProvider(new DateTime(2000, 01, 01));
            var provider = new FakeIsolatorLimitResultProvider();

            Action code = () =>
            {
                _timeMonitorEvent.WaitOne();
                timeProvider.Advance(TimeSpan.FromMinutes(.99));
                _timeMonitorEvent.WaitOne();
            };
            provider.Consume(timeProvider, code, _timeMonitor);

            Assert.IsEmpty(provider.Invocations);

            // give time to the monitor to register the time consumer ended
            _timeMonitorEvent.WaitOne();
            Assert.AreEqual(0, _timeMonitor.Count);
        }

        [Test]
        public void ConsumesMultipleMinutes()
        {
            var timeProvider = new ManualTimeProvider(new DateTime(2000, 01, 01));
            var provider = new FakeIsolatorLimitResultProvider();

            Action code = () =>
            {
                // lets give the monitor time to register the initial time
                _timeMonitorEvent.WaitOne();
                for (int i = 0; i < 4; i++)
                {
                    timeProvider.AdvanceSeconds(45);
                    // give the monitoring task time to request more time
                    _timeMonitorEvent.WaitOne();
                }
            };

            provider.Consume(timeProvider, code, _timeMonitor);

            Assert.AreEqual(3, provider.Invocations.Count);
            Assert.IsTrue(provider.Invocations.TrueForAll(invoc => invoc == 1));
            // give time to the monitor to register the time consumer ended
            _timeMonitorEvent.WaitOne();
            Assert.AreEqual(0, _timeMonitor.Count);
        }

        private class FakeIsolatorLimitResultProvider : IIsolatorLimitResultProvider
        {
            private List<int> _ivocations = new List<int>();
            public List<int> Invocations
            {
                get
                {
                    lock (_ivocations)
                    {
                        return _ivocations.ToList();
                    }
                }
            }

            public IsolatorLimitResult IsWithinLimit()
            {
                return new IsolatorLimitResult(TimeSpan.Zero, string.Empty);
            }

            public void RequestAdditionalTime(int minutes)
            {
                lock (_ivocations)
                {
                    _ivocations.Add(minutes);
                }
            }

            public bool TryRequestAdditionalTime(int minutes)
            {
                lock (_ivocations)
                {
                    _ivocations.Add(minutes);
                }
                return true;
            }
        }

        private class TimeMonitorTest: TimeMonitor
        {
            public AutoResetEvent TimeMonitorEvent;

            public TimeMonitorTest(int monitorIntervalMs = 100) : base(monitorIntervalMs) { }

            protected override void ProcessConsumer(TimeConsumer consumer)
            {
                base.ProcessConsumer(consumer);

                TimeMonitorEvent.Set();
            }
            protected override void RemoveAll()
            {
                // Store the TimeConsumer objects to remove
                var toRemove = TimeConsumers.Where(time => time.Finished).ToList();

                base.RemoveAll();

                // Check if there were elements removed
                if (toRemove.Any())
                {
                    TimeMonitorEvent.Set();
                }
            }
        }
    }
}
