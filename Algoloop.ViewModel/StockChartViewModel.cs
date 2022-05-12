/*
 * Copyright 2021 Capnode AB
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

using Algoloop.ViewModel.Internal.Lean;
using StockSharp.Algo.Candles;
using StockSharp.Charting;
using System.Collections.Generic;
using System.Windows.Media;

namespace Algoloop.ViewModel
{
    public class StockChartViewModel : IChartViewModel
    {
        public StockChartViewModel(
            string name,
            ChartCandleDrawStyles style,
            System.Drawing.Color color,
            IEnumerable<Candle> candles)
        {
            Title = name;
            Style = style;
            Color = Converters.ToMediaColor(color);
            IsVisible = true;
            Candles = candles;
        }

        public string Title { get; }
        public int SubChart { get; }
        public ChartCandleDrawStyles Style { get; }
        public Color Color { get; }
        public bool IsVisible { get; set; }
        public IEnumerable<Candle> Candles { get; }
    }
}
