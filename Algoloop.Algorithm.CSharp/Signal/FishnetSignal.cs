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
    public class FishnetSignal : ISignal
    {
        private readonly int _period;
        private readonly RollingWindow<BaseData> _window;

        public FishnetSignal(int period)
        {
            _period = period;
            _window = new RollingWindow<BaseData>(period);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            _window.Add(bar);
            float score = 0;
            if (!_window.IsReady) return 0;
            int i = 0;
            decimal sum = 0;
            decimal price = bar.Price;
            foreach (BaseData item in _window)
            {
                i++;
                sum += item.Price;
                decimal avg = sum / i;
                //                        if (price >= item.Close)
                if (price >= avg)
                {
                    score++;
                }
            }

            return score > 0.5 * _period ? 1 : 0;
        }

        public void Done()
        {
            throw new System.NotImplementedException();
        }
    }
}
