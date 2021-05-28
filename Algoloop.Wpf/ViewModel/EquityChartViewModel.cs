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

using Algoloop.Wpf.Common;
using StockSharp.Xaml.Charting;
using System.Collections.Generic;
using System.Windows.Media;

namespace Algoloop.Wpf.ViewModel
{
    public class EquityChartViewModel : IChartViewModel
    {
        public EquityChartViewModel(
            string title,
            ChartIndicatorDrawStyles style,
            System.Drawing.Color color,
            int subChart,
            bool isVisible,
            IEnumerable<EquityData> series)
        {
            Title = title;
            Style = style;
            Color = Converters.ToMediaColor(color);
            SubChart = subChart;
            IsVisible = isVisible;
            Series = series;
        }

        public string Title { get; }
        public ChartIndicatorDrawStyles Style { get; }
        public int SubChart { get; }
        public Color Color { get; }
        public bool IsVisible { get; set; }
        public IEnumerable<EquityData> Series { get; }
    }
}
