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
using QuantConnect.Indicators;
using System;

namespace Algoloop.Algorithm.CSharp.Model
{
    public class SharpeSignal : ISignal
    {
        private readonly RateOfChange _roc;
        private readonly StandardDeviation _std;
        private decimal? _last;

        public SharpeSignal(int period)
        {
            _roc = new RateOfChange(period);
            _std = new StandardDeviation(period - 1);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            decimal close = bar.Price;
            _roc.Update(bar.Time, close);
            if (_last != null)
            {
                decimal last = _last ?? 0;
                decimal diff = (close - last) / last;
                _std.Update(bar.Time, diff);
            }

            _last = close;
            if (!_roc.IsReady) return 0;
            if (!_std.IsReady) return 0;
            //                    Algorithm.Log($"{Algorithm.Time:yyyyMMdd} {bar.Symbol.Value} {FineFundamental.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths:0} {FineFundamental.OperationRatios.RevenueGrowth.OneYear:0.####}");
            float annualizedReturn = 250 * (float)(decimal)_roc / (_roc.Period - 1);
            float annualizedStd = (float)Math.Sqrt(250) * (float)(decimal)_std;
            float score = annualizedStd != 0 ? annualizedReturn / annualizedStd : 0;
            return score;
        }

        public void Done()
        {
        }
    }
}
