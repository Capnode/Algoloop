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
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    public abstract class CommonIndicatorTests<T>
        where T : IBaseData
    {
        protected Symbol Symbol { get; set; } = Symbols.SPY;
        [Test]
        public virtual void ComparesAgainstExternalData()
        {
            var indicator = CreateIndicator();
            RunTestIndicator(indicator);
        }

        [Test]
        public virtual void ComparesAgainstExternalDataAfterReset()
        {
            var indicator = CreateIndicator();
            RunTestIndicator(indicator);
            indicator.Reset();
            RunTestIndicator(indicator);
        }

        [Test]
        public virtual void ResetsProperly()
        {
            var indicator = CreateIndicator();
            if (indicator is IndicatorBase<IndicatorDataPoint>)
                TestHelper.TestIndicatorReset(indicator as IndicatorBase<IndicatorDataPoint>, TestFileName);
            else if (indicator is IndicatorBase<IBaseDataBar>)
                TestHelper.TestIndicatorReset(indicator as IndicatorBase<IBaseDataBar>, TestFileName);
            else if (indicator is IndicatorBase<TradeBar>)
                TestHelper.TestIndicatorReset(indicator as IndicatorBase<TradeBar>, TestFileName);
            else
                throw new NotSupportedException("ResetsProperly: Unsupported indicator data type: " + typeof(T));
        }

        [Test]
        public virtual void WarmsUpProperly()
        {
            var indicator = CreateIndicator();
            var period = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (!period.HasValue)
            {
                Assert.Ignore($"{indicator.Name} is not IIndicatorWarmUpPeriodProvider");
                return;
            }

            var startDate = new DateTime(2019, 1, 1);

            for (var i = 0; i < period.Value; i++)
            {
                var input = GetInput(startDate, i);
                indicator.Update(input);
                Assert.AreEqual(i == period.Value - 1, indicator.IsReady);
            }

            Assert.AreEqual(period.Value, indicator.Samples);
        }

        [Test]
        public virtual void TimeMovesForward()
        {
            var indicator = CreateIndicator();
            var startDate = new DateTime(2019, 1, 1);

            for (var i = 10; i > 0; i--)
            {
                var input = GetInput(startDate, i);
                indicator.Update(input);
            }
            
            Assert.AreEqual(1, indicator.Samples);
        }

        [Test]
        public virtual void AcceptsRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            if (indicator is IndicatorBase<TradeBar> ||
                indicator is IndicatorBase<IBaseData> ||
                indicator is BarIndicator ||
                indicator is IndicatorBase<IBaseDataBar>)
            {
                var renkoConsolidator = new RenkoConsolidator(RenkoBarSize);
                renkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                TestHelper.UpdateRenkoConsolidator(renkoConsolidator, TestFileName);
                Assert.IsTrue(indicator.IsReady);
                Assert.AreNotEqual(0, indicator.Samples);
                IndicatorValueIsNotZeroAfterReceiveRenkoBars(indicator);
                renkoConsolidator.Dispose();
            }
        }

        [Test]
        public virtual void AcceptsVolumeRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            if (indicator is IndicatorBase<TradeBar> ||
                indicator is IndicatorBase<IBaseData> ||
                indicator is BarIndicator ||
                indicator is IndicatorBase<IBaseDataBar>)
            {
                var volumeRenkoConsolidator = new VolumeRenkoConsolidator(VolumeRenkoBarSize);
                volumeRenkoConsolidator.DataConsolidated += (sender, volumeRenkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(volumeRenkoBar));
                };

                TestHelper.UpdateRenkoConsolidator(volumeRenkoConsolidator, TestFileName);
                Assert.IsTrue(indicator.IsReady);
                Assert.AreNotEqual(0, indicator.Samples);
                IndicatorValueIsNotZeroAfterReceiveVolumeRenkoBars(indicator);
                volumeRenkoConsolidator.Dispose();
            }
        }

        [Test]
        public virtual void WorksWithLowValues()
        {
            var indicator = CreateIndicator();
            var period = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            var random = new Random();
            var time = new DateTime(2023, 5, 28);
            for (int i = 0; i < 2 * period; i++)
            {
                var value = (decimal)(random.NextDouble() * 0.000000000000000000000000000001);
                Assert.DoesNotThrow(() => indicator.Update(GetInput(Symbol, time, i, value, value, value, value)));
            }
        }

        protected virtual void IndicatorValueIsNotZeroAfterReceiveRenkoBars(IndicatorBase indicator)
        {
            Assert.AreNotEqual(0, indicator.Current.Value);
        }

        protected virtual void IndicatorValueIsNotZeroAfterReceiveVolumeRenkoBars(IndicatorBase indicator)
        {
            Assert.AreNotEqual(0, indicator.Current.Value);
        }

        protected static IBaseData GetInput(DateTime startDate, int days) => GetInput(Symbols.SPY, startDate, days);

        protected static IBaseData GetInput(Symbol symbol, DateTime startDate, int days) => GetInput(symbol, startDate, days, 100m + days, 105m + days, 95m + days, 100 + days);

        protected static IBaseData GetInput(Symbol symbol, DateTime startDate, int days, decimal open, decimal high, decimal low, decimal close)
        {
            if (typeof(T) == typeof(IndicatorDataPoint))
            {
                return new IndicatorDataPoint(symbol, startDate.AddDays(days), close);
            }

            return new TradeBar(
                startDate.AddDays(days),
                symbol,
                open,
                high,
                low,
                close,
                100m,
                Time.OneDay
            );
        }

        public PyObject GetIndicatorAsPyObject()
        {
            using (Py.GIL())
            {
                return Indicator.ToPython();
            }
        }

        public IndicatorBase<T> Indicator => CreateIndicator();

        /// <summary>
        /// Executes a test of the specified indicator
        /// </summary>
        protected virtual void RunTestIndicator(IndicatorBase<T> indicator)
        {
            if (indicator is IndicatorBase<IndicatorDataPoint>)
                TestHelper.TestIndicator(
                    indicator as IndicatorBase<IndicatorDataPoint>,
                    TestFileName,
                    TestColumnName,
                    Assertion as Action<IndicatorBase<IndicatorDataPoint>, double>
                );
            else if (indicator is IndicatorBase<IBaseDataBar>)
                TestHelper.TestIndicator(
                    indicator as IndicatorBase<IBaseDataBar>,
                    TestFileName,
                    TestColumnName,
                    Assertion as Action<IndicatorBase<IBaseDataBar>, double>
                );
            else if (indicator is IndicatorBase<TradeBar>)
                TestHelper.TestIndicator(
                    indicator as IndicatorBase<TradeBar>,
                    TestFileName,
                    TestColumnName,
                    Assertion as Action<IndicatorBase<TradeBar>, double>);
            else
                throw new NotSupportedException("RunTestIndicator: Unsupported indicator data type: " + typeof(T));
        }

        /// <summary>
        /// Returns a custom assertion function, parameters are the indicator and the expected value from the file
        /// </summary>
        protected virtual Action<IndicatorBase<T>, double> Assertion
        {
            get
            {
                return (indicator, expected) =>
                {
                    Assert.AreEqual(expected, (double)indicator.Current.Value, 1e-3);

                    var relativeDifference = Math.Abs(((double)indicator.Current.Value - expected) / expected);
                    Assert.LessOrEqual(relativeDifference, 1); // less than 1% error rate
                };
            }
        }

        /// <summary>
        /// Returns a new instance of the indicator to test
        /// </summary>
        protected abstract IndicatorBase<T> CreateIndicator();

        /// <summary>
        /// Returns the CSV file name containing test data for the indicator
        /// </summary>
        protected abstract string TestFileName { get; }

        /// <summary>
        /// Returns the name of the column of the CSV file corresponding to the pre-calculated data for the indicator
        /// </summary>
        protected abstract string TestColumnName { get; }

        /// <summary>
        /// Returns the BarSize for the RenkoBar test, namely, AcceptsRenkoBarsAsInput()
        /// </summary>
        protected decimal RenkoBarSize { get; set; } = 10m;

        /// <summary>
        /// Returns the BarSize for the VolumeRenkoBar test, namely, AcceptsVolumeRenkoBarsAsInput()
        /// </summary>
        protected decimal VolumeRenkoBarSize { get; set; } = 500000m;
    }
}
