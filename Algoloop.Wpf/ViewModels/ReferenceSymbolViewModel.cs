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

using Algoloop.Wpf.Model;
using QuantConnect;
using QuantConnect.Data;
using System.Collections.Generic;

namespace Algoloop.Wpf.ViewModels
{
    public class ReferenceSymbolViewModel : ViewModelBase
    {
        public ReferenceSymbolViewModel(ReferenceSymbolModel model, SymbolViewModel symbol)
        {
            Model = model;
            Symbol = symbol;
        }

        public string DisplayName => $"{Model.Market}:{Model.Name}";

        public ReferenceSymbolModel Model { get; set; }

        public SymbolViewModel Symbol { get; set; }

        public Series ChartSeries(SymbolViewModel symbol)
        {
            string name = SeriesName(symbol);
            var series = new Series(name, SeriesType.Line);
            IEnumerable<BaseData> series1 = symbol.History();
            if (series1 == null) return series;
            IEnumerable<BaseData> series2 = Symbol.History(symbol.Market.ChartResolution, symbol.Market.ChartDate);
            if (series2 == null) return series;
            IEnumerator<BaseData> iter1 = series1.GetEnumerator();
            IEnumerator<BaseData> iter2 = series2.GetEnumerator();
            bool is1 = iter1.MoveNext();
            bool is2 = iter2.MoveNext();
            decimal value = 0;
            decimal value0 = 0;
            while (is1 && is2)
            {
                BaseData data1 = iter1.Current;
                BaseData data2 = iter2.Current;
                if (data1.Time < data2.Time)
                {
                    is1 = iter1.MoveNext();
                    continue;
                }

                if (data1.Time > data2.Time)
                {
                    is2 = iter2.MoveNext();
                    continue;
                }

                if (data1.Time == data2.Time)
                {
                    is1 = iter1.MoveNext();
                    is2 = iter2.MoveNext();
                    switch (Model.Operation)
                    {
                        case Operator.None:
                            value = data2.Value;
                            break;
                        case Operator.Versus:
                            if (data2.Value == 0) continue;
                            value = data1.Value / data2.Value;
                            if (value0 == 0)
                            {
                                value0 = value;
                            }
                            value /= value0;
                            break;
                        case Operator.Divide:
                            if (data2.Value == 0) continue;
                            value = data1.Value / data2.Value;
                            break;
                        case Operator.Multiply:
                            value = data1.Value * data2.Value;
                            break;
                        case Operator.Plus:
                            value = data1.Value + data2.Value;
                            break;
                        case Operator.Minus:
                            value = data1.Value - data2.Value;
                            break;
                    }

                    series.AddPoint(data1.Time.ToLocalTime(), value);
                }
            }

            return series;
        }

        private string SeriesName(SymbolViewModel symbol)
        {
            switch (Model.Operation)
            {
                case Operator.None: return Model.Name;
                case Operator.Versus: return $"{symbol.Model.Name} vs {Symbol.Model.Name}";
                case Operator.Multiply: return $"{symbol.Model.Name} * {Symbol.Model.Name}";
                case Operator.Divide: return $"{symbol.Model.Name} / {Symbol.Model.Name}";
                case Operator.Plus: return $"{symbol.Model.Name} + {Symbol.Model.Name}";
                case Operator.Minus: return $"{symbol.Model.Name} - {Symbol.Model.Name}";
                default: return Model.Name;
            }
        }
    }
}
