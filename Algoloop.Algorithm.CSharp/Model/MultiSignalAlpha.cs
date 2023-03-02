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

using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using System.Diagnostics;

namespace Algoloop.Algorithm.CSharp.Model
{
    public class MultiSignalAlpha : AlphaModel
    {
        private readonly bool LogSignal = false;
        private readonly bool LogInsights = false;

        private readonly InsightDirection _direction;
        private readonly Resolution _resolution;
        private readonly int _hold;
        private readonly IEnumerable<Symbol> _symbols;
        private readonly Func<Symbol, ISignal>[] _factories;
        private readonly IDictionary<Symbol, ISignal[]> _signals = new Dictionary<Symbol, ISignal[]>();

        public MultiSignalAlpha(
            InsightDirection direction,
            Resolution resolution,
            int hold,
            IEnumerable<Symbol> symbols,
            params Func<Symbol, ISignal>[] factories)
        {
            _direction = direction;
            _resolution = resolution;
            _hold = hold;
            _symbols = symbols;
            _factories = factories;

            Name = GetType().Name;
        }

        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            if (algorithm == default) throw new ArgumentNullException(nameof(algorithm));

            var insights = new List<Insight>();
            foreach (KeyValuePair<Symbol, BaseData> pair in data)
            {
                Symbol symbol = pair.Key;
                Debug.Assert(_symbols.Contains(symbol));
                bool ok = _signals.TryGetValue(symbol, out ISignal[] signals);
                Debug.Assert(ok);
                if (!ok) continue;
                float score = _direction.Equals(InsightDirection.Up) ? 1 : _direction.Equals(InsightDirection.Down) ? -1 : float.NaN;
                foreach (ISignal symbolData in signals)
                {
                    float signal = symbolData.Update(algorithm, pair.Value);
                    if (LogSignal)
                    {
                        algorithm.Log($"{pair.Key} {symbolData.GetType().Name}: {signal}");
                    }

                    if (float.IsNaN(signal)) continue;
                    if (float.IsNaN(score))
                    {
                        score = signal;
                    }
                    else if (score < 0 && signal < 0)
                    {
                        score = - score * signal;
                    }
                    else if (score > 0 && signal > 0)
                    {
                        score *= signal;
                    }
                    else
                    {
                        score = 0;
                    }
                }

                if (float.IsNaN(score) || score == 0) continue;
                TimeSpan period = _resolution.ToTimeSpan().Multiply(_hold).Subtract(TimeSpan.FromTicks(1));
                DateTime closeTimeUtc = algorithm.UtcTime.Add(period);

                InsightDirection direction = InsightDirection.Flat;
                if (score > 0)
                {
                    direction = InsightDirection.Up;
                }
                else if (score < 0)
                {
                    direction = InsightDirection.Down;
                }

                Insight insight = Insight.Price(symbol, closeTimeUtc, direction, Math.Abs(score), null, Name);
                insights.Add(insight);
            }

            // Sort insights decending
            insights.Sort((Insight x, Insight y) => Compare(x,y));
            if (LogInsights)
            {
                DoLogInsights(algorithm, insights);
            }

            return insights;
        }

        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            foreach (Symbol symbol in changes.RemovedSecurities.Select(x => x.Symbol))
            {
            }

            // Initialize data for added securities
            IEnumerable<Symbol> symbols = changes.AddedSecurities.Select(x => x.Symbol);
            if (symbols.Any())
            {
                foreach (Symbol symbol in symbols)
                {
                    if (_symbols.Contains(symbol))
                    {
                        var list = new List<ISignal>();
                        foreach (var factory in _factories)
                        {
                            Debug.Assert(!_signals.ContainsKey(symbol));
                            ISignal signal = factory(symbol);
                            if (signal != default)
                            {
                                list.Add(signal);
                            }
                        }

                        _signals.Add(symbol, list.ToArray());
                    }
                }
            }
        }

        public void OnEndOfAlgorithm()
        {
            foreach (ISignal[] signals in _signals.Values)
            {
                foreach (ISignal signal in signals)
                {
                    signal.Done();
                }
            }
        }

        private static int Compare(Insight x, Insight y)
        {
            if (x == null)
            {
                return 1;
            }

            if (y == null)
            {
                return -1;
            }

            if (x.Magnitude > y.Magnitude)
            {
                return -1;
            }

            if (x.Magnitude < y.Magnitude)
            {
                return 1;
            }

            return 0;
        }

        private static void DoLogInsights(QCAlgorithm algorithm, IEnumerable<Insight> insights)
        {
            int i = 0;
            foreach (Insight insight in insights)
            {
                string text = $"Insight {++i} {insight.Symbol} source={insight.SourceModel} magniture={insight.Magnitude:0.####}".ToStringInvariant();
                if (insight.Confidence != null)
                {
                    text += $" confidence={insight.Confidence:0.####}".ToStringInvariant();
                }

                text += $" {insight.Direction}";
                algorithm.Log(text);
            }
        }
    }
}
