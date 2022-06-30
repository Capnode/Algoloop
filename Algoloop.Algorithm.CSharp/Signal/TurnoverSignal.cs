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

using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System.Linq;

namespace Algoloop.Algorithm.CSharp
{
    internal class TurnoverSignal : ISignal
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly QCAlgorithm _algorithm;
#pragma warning restore IDE0052 // Remove unread private members
        private readonly int _period;
        private readonly long _turnover;
        private readonly RollingWindow<long> _window;

        public TurnoverSignal(QCAlgorithm algorithm, int period, long turnover)
        {
            _algorithm = algorithm;
            _period = period;
            _turnover = turnover;
            if (period > 0)
            {
                _window = new RollingWindow<long>(_period);
            }
        }

        public float Update(BaseData bar, bool evaluate)
        {
            if (_window == null) return float.NaN;
            long turnover = 0;
            if (bar is TradeBar tradebar)
            {
                turnover = (long)(tradebar.Volume * bar.Price);
            }

            _window.Add(turnover);
            if (!evaluate) return 0;
            if (!_window.IsReady) return 0;
            int count = _window.Count(m => m >= _turnover);
            if (count > _period / 2)
            {
                return float.NaN;
            }

            return 0;
        }

        public void Done()
        {
        }
    }
}
