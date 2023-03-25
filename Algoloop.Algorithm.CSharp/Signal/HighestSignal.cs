/*
 * Copyright 2023 Capnode AB
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

using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Algoloop.Algorithm.CSharp.Model
{
    public class HighestSignal : ISignal
    {
        private readonly Maximum _highest;
        private decimal _ath;

        public HighestSignal(int period)
        {
            if (period > 0)
            {
                _highest = new Maximum($"Highest({period})", period);
            }
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            if (bar is not TradeBar tradeBar) return 0;
            
            decimal high = tradeBar.High;
            if (_highest == null)
            {
                if (high > _ath)
                    _ath = high;

                if (algorithm.IsWarmingUp || _ath == 0)
                    return 0;

                return (float)(tradeBar.Close / _ath);
            }

            _highest.Update(bar.Time, high);
            if (algorithm.IsWarmingUp || !_highest.IsReady)
                return 0;

            decimal highest = _highest;
            if (highest == 0)
                return 0;

            return (float)(tradeBar.Close / highest);
        }

        public void Done()
        {
        }
    }
}

