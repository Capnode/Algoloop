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

using QuantConnect;

namespace Algoloop.Wpf.ViewModels
{
    public class ChartViewModel : ViewModelBase, IChartViewModel
    {
        private bool _isVisible;

        public ChartViewModel(Chart chart, bool isVisible)
        {
            Chart = chart;
            IsVisible = isVisible;
        }

        public string Title => Chart.Name;

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public Chart Chart { get; }
    }
}
