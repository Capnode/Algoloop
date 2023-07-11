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
using QuantConnect.Indicators;

namespace Algoloop.Algorithm.CSharp.Signal
{
    internal class ProfitMaSignal : ISignal
    {
        private readonly RollingWindow<float> _window;

        public ProfitMaSignal(int period)
        {
            _window = new RollingWindow<float>(period);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            _window.Add((float)bar.Price);
            if (!_window.IsReady) return 0;

            float first = _window.First();
            float last = _window.Last();
            float ma = _window.Sum() / _window.Count;
            float maDiff = first - ma;
            if (maDiff <= 0) return 0;
            float profit = first - last;
            float score = profit / last;
            return score;
        }

        public void Done()
        {
        }
    }
}
