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

using Algoloop.Algorithm.CSharp.Model;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace Algoloop.Algorithm.CSharp.Signal
{
    public class SmaCrossSignal : ISignal
    {
        private readonly SimpleMovingAverage _fastSma;
        private readonly SimpleMovingAverage _slowSma;
        private DateTime _time;

        public SmaCrossSignal(int fastPeriod, int slowPeriod)
        {
            if (fastPeriod > 0)
            {
                _fastSma = new SimpleMovingAverage(fastPeriod);
            }
            if (slowPeriod > 0)
            {
                _slowSma = new SimpleMovingAverage(slowPeriod);
            }
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            if (bar.Time <= _time) throw new ApplicationException("Duplicate bars");
            _time = bar.Time;
            decimal close = bar.Price;
            //algorithm.Log($"{bar.Time:d} {bar.Symbol.ID.Symbol} {close}");
            _fastSma?.Update(bar.Time, close);
            _slowSma?.Update(bar.Time, close);

            if (_fastSma == null && _slowSma == null) return float.NaN;
            if (_slowSma == null)
            {
                if (!_fastSma.IsReady) return 0;
                return close > _fastSma ? 1 : 0;
            }

            if (_fastSma == null)
            {
                if (!_slowSma.IsReady) return 0;
                return close > _slowSma ? 1 : 0;
            }

            if (_fastSma.Period >= _slowSma.Period) return 0;
            if (!_fastSma.IsReady || !_slowSma.IsReady) return 0;
            //algorithm.Log($"{bar.Time:d} {bar.Symbol.ID.Symbol} _fastSma={_fastSma} _slowSma={_slowSma}");
            return _fastSma > _slowSma ? 1 : 0;
        }

        public void Done()
        {
        }
    }
}

