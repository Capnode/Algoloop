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

using Algoloop.Algorithm.CSharp.Model;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace Algoloop.Algorithm.CSharp.Signal
{
    public class MacdSignal : ISignal
    {
        private readonly int _signal;
        private readonly MovingAverageConvergenceDivergence _macd;
        private readonly int _fast;
        private readonly int _slow;

        public MacdSignal(QCAlgorithm algorithm, Symbol symbol, Resolution resolution, int fast, int slow, int signal)
        {
            _fast = fast;
            _slow = slow;
            _signal = signal;
            if (fast == 0 || slow == 0 || fast >= slow) return;
            _macd = algorithm.MACD(
                symbol,
                fast,
                slow,
                signal,
                MovingAverageType.Exponential,
                resolution,
                Field.Close);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            if (_fast == 0 && _slow == 0 && _signal == 0) return 1; // Bypass signal
            if (_macd == default) return 0;
            if (!_macd.IsReady) return 0;
            if (_signal == 0)
            {
                return _macd > 0 ? 1 : 0;
            }

            return _macd > _macd.Signal ? 1 : 0;
        }
    }
}
