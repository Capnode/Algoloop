/*
 * Copyright 2023 Capnode AB
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
using QuantConnect.Benchmarks;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace Algoloop.Algorithm.CSharp.Model
{
    public class BenchmarkTrendSignal : ISignal
    {
        private string _symbolName;
        private string _benchmarkName;
        private decimal _benchmark0;
        private decimal _price0;
        private readonly Minimum _trackerLow;

        public BenchmarkTrendSignal(int period)
        {
            if (period > 0)
            {
                _trackerLow = new Minimum($"Tracker Low({period})", period);
            }
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            if (string.IsNullOrEmpty(_symbolName))
            {
                _symbolName = bar.Symbol.ID.Symbol;
            }

            if (string.IsNullOrEmpty(_benchmarkName))
            {
                if (algorithm.Benchmark is SecurityBenchmark sec)
                {
                    _benchmarkName = sec.Security.Symbol.ID.Symbol;
                }
                else
                {
                    _benchmarkName = "Benchmark";
                }
            }

            decimal benchmark = algorithm.Benchmark.Evaluate(algorithm.Time);
            decimal price = bar.Price;
            if (_benchmark0 == 0)
            {
                _benchmark0 = benchmark;
            }
            if (_benchmark0 == 0)
                return 0;

            if (_price0 == 0)
            {
                _price0 = price;
            }
            if (_price0 == 0)
                return 0;

            decimal index = 0;
            decimal iPrice = price / _price0;
            decimal iBenchmark = benchmark / _benchmark0;
            if (iBenchmark > 0)
            {
                index = iPrice / iBenchmark;
                algorithm.Plot($"{_symbolName} / {_benchmarkName}", index);
            }

            if (_trackerLow == null)
                return (float)index;

            _trackerLow.Update(bar.Time, index);
            if (!_trackerLow.IsReady)
                return 0;

            if (algorithm.IsWarmingUp)
                return 0;

            return index < _trackerLow ? 0 : (float)index;
        }

        public void Done()
        {
        }
    }
}

