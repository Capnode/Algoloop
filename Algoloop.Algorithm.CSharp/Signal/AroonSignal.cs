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

using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace Algoloop.Algorithm.CSharp.Model
{
    public class AroonSignal : ISignal
    {
        private readonly AroonOscillator _aroon;

        public AroonSignal(QCAlgorithm algorithm, Symbol symbol, Resolution resolution, int period)
        {
            _aroon = algorithm.AROON(symbol, period, resolution);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            if (!_aroon.IsReady) return 0;
            return decimal.Compare(_aroon.AroonUp, _aroon.AroonDown);
        }

        public void Done()
        {
        }
    }
}
