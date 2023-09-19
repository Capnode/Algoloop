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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Linq;

namespace Algoloop.Algorithm.CSharp.Signal
{
    public class BreakoutTimeframeSignal : ISignal, IDisposable
    {
        private bool _isDisposed = false; // To detect redundant calls
        private readonly QCAlgorithm _algorithm;
        private readonly Symbol _symbol;
        private readonly RollingWindow<TradeBar> _rangeWindow;
        private readonly TradeBarConsolidator _consolidator;
        private float _score;

        public BreakoutTimeframeSignal(QCAlgorithm algorithm, Symbol symbol, Resolution resolution, int timeframe, int period)
        {
            _algorithm = algorithm;
            _symbol = symbol;
            _rangeWindow = new RollingWindow<TradeBar>(period);
            if (resolution.Equals(Resolution.Daily) && timeframe.Equals(5))
            {
                _consolidator = new TradeBarConsolidator(Calendar.Weekly);
            }
            else if (resolution.Equals(Resolution.Daily) && timeframe.Equals(22))
            {
                _consolidator = new TradeBarConsolidator(Calendar.Monthly);
            }
            else
            {
                _consolidator = new TradeBarConsolidator(timeframe);
            }

            _consolidator.DataConsolidated += (sender, consolidated) =>
            {
                _rangeWindow.Add(consolidated);
                _score = 0;
                if (_rangeWindow.IsReady)
                {
                    decimal highestLow = _rangeWindow.Select(x => x.Low).Max();
                    _score = decimal.Compare(consolidated.Close, highestLow);
                }
            };

            algorithm.SubscriptionManager.AddConsolidator(symbol, _consolidator);
        }

        ~BreakoutTimeframeSignal()
        {
            Dispose(false);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            return _score;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _algorithm.SubscriptionManager.RemoveConsolidator(_symbol, _consolidator);
                    _consolidator.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}
