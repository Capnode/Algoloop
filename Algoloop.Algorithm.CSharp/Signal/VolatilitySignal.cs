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

using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace Algoloop.Algorithm.CSharp
{
    internal class VolatilitySignal : ISignal
    {
        private readonly RollingWindow<float> _window;
        private BaseData _last;

        public VolatilitySignal(int period)
        {
            _window = new RollingWindow<float>(period - 1);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            if (_last != null)
            {
                decimal diff = (bar.Price - _last.Price) / _last.Price;
                _window.Add((float)diff);
            }

            _last = bar;
            if (!_window.IsReady) return 0;

            float profit = _window.ToList().Aggregate((x, y) => (1 + x) * (1 + y) - 1);
            int count = _window.Count;
            float avg = profit / count;
            float sum = _window.Sum(d => (d - avg) * (d - avg));
            float var = sum / count;
            float score = var != 0 ? profit /var : 0;
            return score;
        }

        public void Done()
        {
        }
    }
}
