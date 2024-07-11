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
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class ValueAtRiskTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        private const int _tradingDays = 252;
        
        protected override string TestFileName => "spy_valueatrisk.csv";

        protected override string TestColumnName => "VaR_99";

        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new ValueAtRisk(_tradingDays, 0.99d);
        }

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 1e-3); }
        }

        [Test]
        public void ComparesAgainstExternalData95()
        {
            var indicator = new ValueAtRisk(_tradingDays, 0.95);

            TestHelper.TestIndicator(indicator, TestFileName, "VaR_95", Assertion);
        }

        [Test]
        public void ComparesAgainstExternalData90()
        {
            var indicator = new ValueAtRisk(_tradingDays, 0.9d);

            TestHelper.TestIndicator(indicator, TestFileName, "VaR_90", Assertion);
        }

        [Test]
        public void DivisonByZero()
        {
            var indicator = CreateIndicator();

            for (int i = 0; i < _tradingDays; i++)
            {
                var indicatorDataPoint = new IndicatorDataPoint(new DateTime(), 0);
                indicator.Update(indicatorDataPoint);
            }

            Assert.AreEqual(indicator.Current.Value, 0m);
            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public void PeriodBelowMinimumThrows()
        {
            var period = 2; 

            var exception = Assert.Throws<ArgumentException>(() => new ValueAtRisk(period, 0.99d));
            Assert.That(exception.Message, Is.EqualTo($"Period parameter for ValueAtRisk indicator must be greater than 2 but was {period}"));
        }
    }
}

