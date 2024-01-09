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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Algoloop.Algorithm.CSharp.Signal
{
    internal class TurnoverSignal : ISignal
    {
        private readonly int _period;
        private readonly long _turnover;
        private readonly RollingWindow<long> _window;

        public TurnoverSignal(int period, long turnover)
        {
            _period = period;
            _turnover = turnover;
            if (period > 0)
            {
                _window = new RollingWindow<long>(_period);
            }
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            if (_window == null) return 1;
            long turnover = 0;
            if (bar is TradeBar tradebar)
            {
                turnover = (long)(tradebar.Volume * bar.Price);
            }

            if (turnover > 0)
            {
                _window.Add(turnover);
            }

            if (!_window.IsReady) return 0;
            int count = _window.Count(m => m >= _turnover);
            return count < _period ? 0 : 1;
        }
    }
}
