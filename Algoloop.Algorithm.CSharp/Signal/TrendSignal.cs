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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Algoloop.Algorithm.CSharp.Model
{
    public class TrendSignal : ISignal
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly Minimum _min;
        private readonly ExponentialMovingAverage _ema1;
        private readonly Momentum _mom;
        private readonly Trix _trix;
        private readonly PivotPointsHighLow _pphl;
#pragma warning restore IDE0052 // Remove unread private members
        private readonly int _period1;
        private readonly int _period2;
        private readonly ExponentialMovingAverage _ema2;

        public TrendSignal(QCAlgorithm algorithm, Symbol symbol, Resolution resolution, int period1, int period2)
        {
            _period1 = period1;
            _period2 = period2;
            if (period1 > 0)
            {
                _min = algorithm.MIN(symbol, period1, resolution, Field.Low);
                _ema1 = algorithm.EMA(symbol, period1, resolution, Field.Close);
                _mom = algorithm.MOM(symbol, period1, resolution, Field.Close);
                _trix = algorithm.TRIX(symbol, period1, resolution, Field.Close);
                _pphl = algorithm.PPHL(symbol, (period1 - 1) / 2, (period1 - 1) / 2);
            }
            if (period2 > 0)
            {
                _ema2 = algorithm.EMA(symbol, period2, resolution, Field.Close);
            }
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var tradebar = bar as TradeBar;
#pragma warning restore IDE0059 // Unnecessary assignment of a value

            // Top result: sharpe=2.276, Period=0, Trades=1804

            // Support line
            // Top result: sharpe=2.245, Period=110
            //if (_min == null) return float.NaN;
            //if (!_min.IsReady) return 0;
            //return _min < tradebar.Low ? 1 : 0;

            // Positive Momentum
            // Top result: sharpe=2.153, Period=250
            //if (_mom == null) return float.NaN;
            //if (!_mom.IsReady) return 0;
            //return _mom > 0 ? 1 : 0;

            // Price above SMA
            // Top result: sharpe=2.147, Period=300
            //if (_sma == null) return float.NaN;
            //if (!_sma.IsReady) return 0;
            //return bar.Price > _sma ? 1 : 0;

            // EMA cross
            // Top result: sharpe=2.147, Period=300
            if (_ema1 == null && _ema2 == null) return float.NaN;
            if (_ema1 == null || _ema2 == null) return 0;
            if (_period1 >= _period2) return 0;
            if (!_ema1.IsReady || !_ema2.IsReady) return 0;
            return _ema1 > _ema2 ? 1 : 0;

            // Positive Trix
            // Top result: sharpe=2.281, Period=50
            //if (_trix == null) return float.NaN;
            //if (!_trix.IsReady) return 0;
            //return _trix > 0 ? 1 : 0;

            // Above latest low pivot
            // Top result: sharpe=2.276, Period=50
            //if (_pphl == null) return float.NaN;
            //if (!_pphl.IsReady) return 0;
            //PivotPoint[] lowpoints = _pphl.GetLowPivotPointsArray();
            //_algorithm.Log(bar.Symbol + "Lows: " + string.Join("; ", lowpoints.Select(m => $"{m.Time.AddSeconds(-1).ToShortDateString()} {m.Price.NormalizeToStr()}")));
            //if (lowpoints.Length < 1) return 1;
            //return lowpoints[0].Price <= tradebar.Price ? 1 : 0;

            // Higher highs and higher lows
            // Top result: sharpe=2.213, Period=300
            //if (_pphl == null) return float.NaN;
            //if (!_pphl.IsReady) return 0;
            //PivotPoint[] lowpoints = _pphl.GetLowPivotPointsArray();
            //PivotPoint[] highpoints = _pphl.GetHighPivotPointsArray();
            ////_algorithm.Log(bar.Symbol + "Lows: " + string.Join("; ", lowpoints.Select(m => $"{m.Time.AddSeconds(-1).ToShortDateString()} {m.Price.NormalizeToStr()}")));
            ////_algorithm.Log(bar.Symbol + "Highs: " + string.Join("; ", highpoints.Select(m => $"{m.Time.AddSeconds(-1).ToShortDateString()} {m.Price.NormalizeToStr()}")));
            //if (lowpoints.Length > 0 && lowpoints[0].Price > tradebar.Price) return 0;
            //if (lowpoints.Length > 1 && lowpoints[0].Price < lowpoints[1].Price) return 0;
            //if (highpoints.Length > 1 && highpoints[0].Price < highpoints[1].Price) return 0;
            //return 1;
        }

        public void Done()
        {
        }
    }
}

