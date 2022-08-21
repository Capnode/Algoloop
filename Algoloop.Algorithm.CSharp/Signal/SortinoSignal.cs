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

using QuantConnect.Data;
using QuantConnect.Indicators;
using System;
using System.Linq;

namespace Algoloop.Algorithm.CSharp
{
    internal class SortinoSignal : ISignal
    {
        private readonly RollingWindow<float> _window;
        private BaseData _last;

        public SortinoSignal(int period)
        {
            _window = new RollingWindow<float>(period - 1);
        }

        public float Update(BaseData bar, bool evaluate)
        {
            if (_last != null)
            {
                decimal diff = bar.Price - _last.Price;
                _window.Add((float)diff);
            }
            _last = bar;

            if (!evaluate) return 0;
            if (!_window.IsReady) return 0;
            float profit = _window.Sum();
            int count = _window.Count();
            float avg = profit / count;
            float sum2 = _window.Sum(d => d < 0 ? (d - avg) * (d - avg) : 0);
            float var = sum2 / count;
            float std = (float)Math.Sqrt(var);
            float score = std != 0 ? profit / std : 0;
            return score;
        }

        public void Done()
        {
        }
    }
}