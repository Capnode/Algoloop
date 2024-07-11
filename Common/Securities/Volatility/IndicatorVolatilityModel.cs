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
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Securities.Volatility;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides an implementation of <see cref="IVolatilityModel"/> that uses an indicator
    /// to compute its value
    /// </summary>
    public class IndicatorVolatilityModel : BaseVolatilityModel
    {
        private readonly IIndicator _indicator;
        private readonly Action<Security, BaseData, IIndicator> _indicatorUpdate;

        /// <summary>
        /// Gets the volatility of the security as a percentage
        /// </summary>
        public override decimal Volatility
        {
            get { return _indicator.Current.Value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IVolatilityModel"/> using
        /// the specified <paramref name="indicator"/>. The <paramref name="indicator"/>
        /// is assumed to but updated externally from this model, such as being registered
        /// into the consolidator system.
        /// </summary>
        /// <param name="indicator">The auto-updating indicator</param>
        public IndicatorVolatilityModel(IIndicator indicator)
        {
            _indicator = indicator;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IVolatilityModel"/> using
        /// the specified <paramref name="indicator"/>. The <paramref name="indicator"/>
        /// is assumed to but updated externally from this model, such as being registered
        /// into the consolidator system.
        /// </summary>
        /// <param name="indicator">The auto-updating indicator</param>
        /// <param name="indicatorUpdate">Function delegate used to update the indicator on each call to <see cref="Update"/></param>
        public IndicatorVolatilityModel(IIndicator indicator, Action<Security, BaseData, IIndicator> indicatorUpdate)
        {
            _indicator = indicator;
            _indicatorUpdate = indicatorUpdate;
        }

        /// <summary>
        /// Updates this model using the new price information in
        /// the specified security instance
        /// </summary>
        /// <param name="security">The security to calculate volatility for</param>
        /// <param name="data">The new piece of data for the security</param>
        public override void Update(Security security, BaseData data)
        {
            if (_indicatorUpdate != null)
            {
                _indicatorUpdate(security, data, _indicator);
            }
        }
    }
}
