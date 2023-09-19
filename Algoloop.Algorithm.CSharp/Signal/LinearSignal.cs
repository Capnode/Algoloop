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
    public class LinearSignal : ISignal
    {
        private readonly RollingWindow<float> _win;

        public LinearSignal(int period)
        {
            _win = new RollingWindow<float>(period);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            decimal close = bar.Price;
            _win.Add((float)close);
            if (!_win.IsReady) return 0;
            int count = _win.Count - 1;
            float init = _win[count];
            float netProfit = _win[0] - init;
            float mean = netProfit / count;
            float ideal = _win[0];
            float loss = 0;
            foreach (float price in _win)
            {
                float diff = price - ideal;
                loss += Math.Abs(diff);
                ideal -= mean;
            }

            float score = loss != 0 ? count * netProfit / loss : 0;
            return score;
        }
    }
}
