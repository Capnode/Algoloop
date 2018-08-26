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

using Algoloop.Charts;
using Algoloop.Model;
using Algoloop.Service;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Algoloop.ViewModel
{
    public class StrategyJobViewModel: ViewModelBase
    {
        private StrategyViewModel _parent;
        private IAppDomainService _appDomainService;
        private CancellationTokenSource _cancel;
        private LiveCharts.Wpf.Series _selectedSeries;

        public StrategyJobViewModel(StrategyViewModel parent, StrategyJobModel model, IAppDomainService appDomainService)
        {
            _parent = parent;
            Model = model;
            _appDomainService = appDomainService;

            DeleteJobCommand = new RelayCommand(() => DeleteJob(), true);
            EnabledCommand = new RelayCommand(() => OnEnable(Model.Enabled), true);

            DataFromModel();
        }

        public StrategyJobModel Model { get; }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();

        public SyncObservableCollection<ParameterViewModel> Parameters { get; } = new SyncObservableCollection<ParameterViewModel>();

        public SyncObservableCollection<StatisticViewModel> Statistics { get; } = new SyncObservableCollection<StatisticViewModel>();

        public SyncObservableCollection<Order> Orders { get; } = new SyncObservableCollection<Order>();

        public SeriesCollection ChartCollection { get; private set; } = new SeriesCollection();

        public SeriesCollection SelectedCollection { get; private set; } = new SeriesCollection();

        public DateTime InitialDateTime { get; set; }

        public AxesCollection YAxesCollection { get; } = new AxesCollection();

        public VisualElementsCollection VisualElementsCollection { get; } = new VisualElementsCollection();

        public RelayCommand DeleteJobCommand { get; }

        public RelayCommand EnabledCommand { get; }

        public string Logs
        {
            get => Model.Logs;
        }

        public int Loglines
        {
            get => Logs == null ? 0 :  Logs.Count(m => m.Equals('\n'));
        }


        public bool Enabled
        {
            get => Model.Enabled;
            set
            {
                Model.Enabled = value;
                RaisePropertyChanged(() => Enabled);
            }
        }

        public LiveCharts.Wpf.Series SelectedChart
        {
            get { return _selectedSeries; }
            set
            {
                _selectedSeries = value;
                RaisePropertyChanged();
                if (SelectedCollection != null)
                {
                    SelectedCollection.Clear();
                    if (value != null)
                    {
                        SelectedCollection.Add(value);
                    }

                    RaisePropertyChanged(() => SelectedCollection);
                }
            }
        }

        private async void OnEnable(bool value)
        {
            if (value)
            {
                await StartTaskAsync();
            }
            else
            {
                StopTask();
            }
        }

        internal async Task StartTaskAsync()
        {
            _cancel = new CancellationTokenSource();
            DataToModel();
            await Task.Run(() =>_appDomainService.Run(Model), _cancel.Token);
            DataFromModel();
            _cancel = null;
            Enabled = false;
        }

        private void StopTask()
        {
            if (_cancel != null)
            {
                _cancel.Cancel();
            }
        }

        private void DeleteJob()
        {
            _cancel?.Cancel();
            ChartCollection = null;
            SelectedCollection = null;
            _parent?.DeleteJob(this);
        }

        internal void DataToModel()
        {
            Model.DataFolder = Properties.Settings.Default.DataFolder;

            Model.Symbols.Clear();
            foreach (SymbolViewModel symbol in Symbols)
            {
                Model.Symbols.Add(symbol.Model);
            }

            Model.Parameters.Clear();
            foreach (ParameterViewModel parameter in Parameters)
            {
                Model.Parameters.Add(parameter.Model);
            }
        }

        internal void DataFromModel()
        {
            Symbols.Clear();
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                var symbolViewModel = new SymbolViewModel(null, symbolModel);
                Symbols.Add(symbolViewModel);
            }

            Parameters.Clear();
            foreach (ParameterModel parameterModel in Model.Parameters)
            {
                var parameterViewModel = new ParameterViewModel(null, parameterModel);
                Parameters.Add(parameterViewModel);
            }

            Statistics.Clear();
            Orders.Clear();

            YAxesCollection.Clear();
            VisualElementsCollection.Clear();
            SelectedCollection.Clear();
            ChartCollection.Clear();

            if (Model.Result != null)
            {
                // Allow proper decoding of orders.
                var result = JsonConvert.DeserializeObject<BacktestResult>(Model.Result, new[] { new OrderJsonConverter() });

                foreach (var item in result.Statistics)
                {
                    var statisticViewModel = new StatisticViewModel { Name = item.Key, Value = item.Value };
                    Statistics.Add(statisticViewModel);
                }

                foreach (var item in result.RuntimeStatistics)
                {
                    var statisticViewModel = new StatisticViewModel { Name = item.Key, Value = item.Value };
                    Statistics.Add(statisticViewModel);
                }

                foreach (var order in result.Orders.OrderBy(o => o.Key))
                {
                    Orders.Add(order.Value);
                }

                ParseCharts(result.Charts.MapToChartDefinitionDictionary());
            }

            RaisePropertyChanged(() => Logs);
            RaisePropertyChanged(() => Loglines);
        }

        private void ParseCharts(Dictionary<string, ChartDefinition> charts)
        {
            var chartParser = new SeriesChartComponent(Model.Resolution);

            try
            {
                foreach (var chart in charts)
                {
                    foreach (var series in chart.Value.Series)
                    {
                        InstantChartPoint a = series.Value.Values.FirstOrDefault();
                        InitialDateTime = a.X;
                        LiveCharts.Wpf.Series scrollSeries = chartParser.BuildSeries(series.Value);
                        chartParser.UpdateSeries(scrollSeries, series.Value);
                        ChartCollection.Add(scrollSeries);
                    }
                }

                SelectedChart = ChartCollection.FirstOrDefault() as LiveCharts.Wpf.Series;
            }
            catch (Exception e)
            {
                Log.Error($"{e.GetType()}: {e.Message}");
            }
        }
    }
}
