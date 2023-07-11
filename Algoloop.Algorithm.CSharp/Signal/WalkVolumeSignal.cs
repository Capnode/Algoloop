/*
 * Copyright 2019 Capnode AB
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

using Algoloop.Algorithm.CSharp.Model;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Algoloop.Algorithm.CSharp.Signal
{
    internal class WalkVolumeSignal : ISignal
    {
        private readonly bool _usePrice = true;

        private readonly RollingWindow<float> _window;

        public WalkVolumeSignal(int period)
        {
            _window = new RollingWindow<float>(period);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            decimal value = 0;
            if (bar is TradeBar tradeBar)
            {
                decimal diff = tradeBar.Close - tradeBar.Open;
                value = tradeBar.Volume * (_usePrice ?  diff : diff > 0 ? 1 : diff < 0 ? -1 : 0);
            }

            _window.Add((float)value);
            if (!_window.IsReady) return 0;
            float sum = _window.Sum();
            float walk = _window.ToList().Aggregate((x, y) => Math.Abs(x) + Math.Abs(y));
            double score = walk != 0 ? sum / walk : 0;
            return (float)score;
        }

        public void Done()
        {
        }
    }
}
