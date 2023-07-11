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

namespace Algoloop.Algorithm.CSharp.Signal
{
    internal class ImpulseSignal : ISignal
    {
        private TradeBar _last;

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            if (bar is not TradeBar tradeBar) return 0;
            decimal diff = 0;
            if (_last != null)
            {
                diff = -(bar.Price - _last.Price) / _last.Price;
            }
            _last = tradeBar;
            return diff > 0 ? (float)diff : 0;
        }

        public void Done()
        {
        }
    }
}
