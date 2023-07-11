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
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace Algoloop.Algorithm.CSharp.Signal
{
    public class BreakoutSignal : ISignal
    {
        private readonly Identity _close;
        private readonly Maximum _upperLine;
        private readonly Minimum _lowerLine;
        private float _upper;
        private float _lower;

        public BreakoutSignal(QCAlgorithm algorithm, Symbol symbol, Resolution resolution, int period)
        {
            _close = algorithm.Identity(symbol);
            _upperLine = algorithm.MAX(symbol, period, resolution, Field.Low);
            _lowerLine = algorithm.MIN(symbol, period, resolution, Field.High);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            if (!_upperLine.IsReady) return 0;
            if (!_lowerLine.IsReady) return 0;
            float score = 0;
            float close = (float)(decimal)_close;

            if (0 < _lower && _lower < _upper && _upper < close)
            {
                float spread = _upper - _lower;
                score = close / spread;
            }

            _upper = (float)(decimal)_upperLine;
            _lower = (float)(decimal)_lowerLine;

            return score;
        }

        public void Done()
        {
        }
    }
}
