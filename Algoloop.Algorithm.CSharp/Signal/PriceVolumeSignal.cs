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
    internal class PriceVolumeSignal : ISignal
    {
        private readonly RollingWindow<float> _total;
        private readonly RollingWindow<float> _delta;
        private TradeBar _last;

        public PriceVolumeSignal(int period)
        {
            _total = new RollingWindow<float>(period);
            _delta = new RollingWindow<float>(period);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            if (bar is not TradeBar tradeBar) return 0;

            if (_last != null)
            {
                float delta = (float)(tradeBar.Volume * (tradeBar.Close - _last.Close));
                float total = (float)(tradeBar.Volume * tradeBar.Close);
                _delta.Add(delta);
                _total.Add(total);
            }

            _last = tradeBar;

            if (!_delta.IsReady) return 0;
            if (!_total.IsReady) return 0;
            float deltaSum = _delta.Sum();
            float totalSum = _total.Sum();
            float score = totalSum != 0 ? deltaSum / totalSum : 0;
            return score;
        }

        public void Done()
        {
        }
    }
}
