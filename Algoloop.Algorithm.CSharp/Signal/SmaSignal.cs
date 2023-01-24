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

using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace Algoloop.Algorithm.CSharp.Model
{
    public class SmaSignal : ISignal
    {
        private readonly SimpleMovingAverage _sma;
        private DateTime _time;

        public SmaSignal(QCAlgorithm algorithm, Symbol symbol, Resolution resolution, int period)
        {
            if (period > 0)
            {
                _sma = algorithm.SMA(symbol, period, resolution);
            }
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
//            algorithm.Log($"{bar.Time:d} {bar.Price}");
            if (bar.Time <= _time) throw new ApplicationException("Duplicate bars");
            _time = bar.Time;
            if (_sma == null) return float.NaN;
            if (!_sma.IsReady) return 0;
            return decimal.Compare(bar.Price, _sma);
        }

        public void Done()
        {
        }
    }
}
