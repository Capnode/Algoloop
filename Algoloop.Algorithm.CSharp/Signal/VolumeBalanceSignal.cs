/*
 * Copyright 2020 Capnode AB
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

namespace Algoloop.Algorithm.CSharp
{
    internal class VolumeBalanceSignal : ISignal
    {
        private readonly RollingWindow<decimal> _window;
        private TradeBar _last;

        public VolumeBalanceSignal(int period)
        {
            _window = new RollingWindow<decimal>(period);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            if (bar is not TradeBar tradeBar) return 0;
            if (_last != null)
            {
                decimal diff = 0;
                if (bar.Price > _last.Price)
                {
                    diff = tradeBar.Volume;
                }
                else if (bar.Price < _last.Price)
                {
                    diff = -tradeBar.Volume;
                }

                _window.Add(diff);
            }

            _last = tradeBar;
            if (!_window.IsReady) return 0;
            decimal sum = 0;
            decimal absSum = 0;
            foreach (decimal volume in _window)
            {
                sum += volume;
                absSum  += Math.Abs(volume);
            }

            if (absSum == 0) return 0;
            decimal volumeBalance = sum / absSum;
            return (float)Math.Max(0, volumeBalance);
        }

        public void Done()
        {
        }
    }
}
