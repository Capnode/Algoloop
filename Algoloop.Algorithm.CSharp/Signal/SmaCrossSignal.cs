/*
 * Copyright 2022 Capnode AB
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
    public class SmaCrossSignal : ISignal
    {
        private readonly SimpleMovingAverage _fastSma;
        private readonly SimpleMovingAverage _slowSma;
        private readonly QCAlgorithm _algorithm;
        private readonly int _fastPeriod;
        private readonly int _slowPeriod;

        public SmaCrossSignal(QCAlgorithm algorithm, Symbol symbol, Resolution resolution, int fastPeriod, int slowPeriod)
        {
            _algorithm = algorithm;
            _fastPeriod = fastPeriod;
            _slowPeriod = slowPeriod;
            if (fastPeriod > 0)
            {
                _fastSma = new SimpleMovingAverage(fastPeriod);
            }
            if (slowPeriod > 0)
            {
                _slowSma = new SimpleMovingAverage(slowPeriod);
            }
        }

        public float Update(BaseData bar, bool evaluate)
        {
            decimal close = bar.Price;
//            string action = evaluate ? "Evaluate" : "Update";
//            _algorithm.Log($"{action} {bar.Time:d} {close}");
            _fastSma.Update(bar.Time, close);
            _slowSma.Update(bar.Time, close);

            if (!evaluate) return 0;
            if (_fastSma == null && _slowSma == null) return float.NaN;
            if (_fastSma == null || _slowSma == null) return 0;
            if (_fastPeriod >= _slowPeriod) return 0;
            if (!_fastSma.IsReady || !_slowSma.IsReady) return 0;
//            _algorithm.Log($"_fastEma={_fastEma} _slowEma={_slowEma}");
            return _fastSma > _slowSma ? 1 : 0;
        }

        public void Done()
        {
        }
    }
}

