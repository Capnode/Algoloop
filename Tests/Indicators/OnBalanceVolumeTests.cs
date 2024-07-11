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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class OnBalanceVolumeTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            return new OnBalanceVolume();
        }

        protected override string TestFileName => "spy_with_obv.txt";

        protected override string TestColumnName => "OBV";

        protected override Action<IndicatorBase<TradeBar>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(
                    expected.ToStringInvariant("0.##E-00"),
                    indicator.Current.Value.ToStringInvariant("0.##E-00")
                );

        /// <summary>
        /// The final value of this indicator is zero because it uses the Volume of the bars it receives.
        /// Since RenkoBar's don't always have Volume, the final current value is zero. Therefore we
        /// skip this test
        /// </summary>
        /// <param name="indicator"></param>
        protected override void IndicatorValueIsNotZeroAfterReceiveRenkoBars(IndicatorBase indicator)
        {
        }
    }
}
