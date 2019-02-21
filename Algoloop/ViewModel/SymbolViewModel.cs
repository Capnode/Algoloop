/*
 * Copyright 2018 Capnode AB
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

using Algoloop.Model;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.ToolBox;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Algoloop.ViewModel
{
    public class SymbolViewModel : ViewModelBase, ITreeViewModel
    {
        private ITreeViewModel _parent;
        private Resolution _selectedResolution = Resolution.Daily;

        public SymbolViewModel(ITreeViewModel parent, SymbolModel model)
        {
            _parent = parent;
            Model = model;

            DeleteCommand = new RelayCommand(() => { }, () => false);
            StartCommand = new RelayCommand(() => { }, () => false);
            StopCommand = new RelayCommand(() => { }, () => false);
            UpdateCommand = new RelayCommand(() => LoadChart(_parent as MarketViewModel), () => _parent is MarketViewModel);
        }

        public RelayCommand DeleteCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand UpdateCommand { get; }

        public SymbolModel Model { get; }
        public IEnumerable<Resolution> ResolutionList { get; } = new[] { Resolution.Daily, Resolution.Hour, Resolution.Minute, Resolution.Second, Resolution.Tick };
        private SyncObservableCollection<ChartViewModel> _charts = new SyncObservableCollection<ChartViewModel>();
        private DateTime _date = DateTime.Today;

        public bool Active
        {
            get => Model.Active;
            set
            {
                Model.Active = value;
                RaisePropertyChanged(() => Active);
                (_parent as StrategyViewModel)?.Refresh();
                (_parent as MarketViewModel)?.Refresh();
            }
        }

        public Resolution SelectedResolution
        {
            get => _selectedResolution;
            set => Set(ref _selectedResolution, value);
        }

        public DateTime Date
        {
            get => _date;
            set => Set(ref _date, value);
        }

        public SyncObservableCollection<ChartViewModel> Charts
        {
            get => _charts;
            set => Set(ref _charts, value);
        }

        public void Refresh()
        {
            Model.Refresh();

            if (_parent is MarketViewModel market)
            {
                LoadChart(market);
            }
        }

        private void LoadChart(MarketViewModel market)
        {
            Debug.Assert(market != null);
            Charts.Clear();
            var charts = Charts;
            var dataType = LeanData.GetDataType(SelectedResolution, TickType.Quote);
            var symbol = new Symbol(SecurityIdentifier.GenerateForex(Model.Name, Market.Dukascopy), Model.Name);
            var config = new SubscriptionDataConfig(dataType, symbol, SelectedResolution, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Quote);
            var sb = new StringBuilder();
            var leanDataReader = new LeanDataReader(config, symbol, SelectedResolution, _date, market.DataFolder);

            var series = new Series(Model.Name, SeriesType.Candle, "$", Color.Black);
            List<BaseData> data = leanDataReader.Parse().ToList();
            if (!data.Any())
                return;

            var viewModel = new ChartViewModel(series, data);
            charts.Add(viewModel);
            Charts = null;
            Charts = charts;
        }
    }
}
