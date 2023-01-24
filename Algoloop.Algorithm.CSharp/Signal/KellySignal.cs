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

namespace Algoloop.Algorithm.CSharp.Model
{
    public class KellySignal : ISignal
    {
        private readonly RateOfChange _roc;
        private readonly Variance _var;
        private decimal? _last;

        public KellySignal(int period)
        {
//                _roc = algorithm.ROC(symbol, parameters.Period, parameters.Resolution, Field.Close);
            _roc = new RateOfChange(period);
            _var = new Variance(period - 1);
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            decimal close = bar.Price;
            _roc.Update(bar.Time, close);
            if (_last != null)
            {
                decimal last = _last ?? 0;
                decimal diff = (close - last) / last;
                _var.Update(bar.Time, diff);
            }

            _last = close;
            if (!_roc.IsReady) return 0;
            if (!_var.IsReady) return 0;
//                    Algorithm.Log($"{Algorithm.Time:yyyyMMdd} {bar.Symbol.Value} {FineFundamental.FinancialStatements.IncomeStatement.TotalRevenue.TwelveMonths:0} {FineFundamental.OperationRatios.RevenueGrowth.OneYear:0.####}");
            float mean = (float)(decimal)_roc / (_roc.Period - 1);
            float variance = (float)(decimal)_var;
            float score = variance != 0 ? mean / variance : 0;
            return score;
        }

        public void Done()
        {
            throw new System.NotImplementedException();
        }
    }
}
