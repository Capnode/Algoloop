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
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Algoloop.ViewModel
{
    public class StrategyJobViewModel: ViewModelBase
    {
        private StrategyViewModel _parent;
        private readonly SettingsModel _settingsModel;
        private CancellationTokenSource _cancel;
        private Isolated<LeanEngine> _leanEngine;
        private StrategyJobModel _model;
        public ChartViewModel _selectedChart;

        public StrategyJobViewModel(StrategyViewModel parent, StrategyJobModel model, SettingsModel settingsModel)
        {
            _parent = parent;
            Model = model;
            _settingsModel = settingsModel;

            StartJobCommand = new RelayCommand(() => OnStartJobCommand(), () => !Enabled);
            StopJobCommand = new RelayCommand(() => OnStopJobCommand(false), () => Enabled);
            DeleteJobCommand = new RelayCommand(() => DeleteJob(), () => !Enabled);
            EnabledCommand = new RelayCommand(() => OnEnableCommand(Model.Enabled), true);

            DataFromModel();
        }

        public StrategyJobModel Model
        {
            get => _model;
            set => Set(ref _model, value);
        }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();

        public SyncObservableCollection<ParameterViewModel> Parameters { get; } = new SyncObservableCollection<ParameterViewModel>();

        public SyncObservableCollection<StatisticViewModel> Statistics { get; } = new SyncObservableCollection<StatisticViewModel>();

        public SyncObservableCollection<Order> Orders { get; } = new SyncObservableCollection<Order>();

        public SyncObservableCollection<ChartViewModel> Charts { get; } = new SyncObservableCollection<ChartViewModel>();

        public RelayCommand StartJobCommand { get; }

        public RelayCommand StopJobCommand { get; }

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
                StartJobCommand.RaiseCanExecuteChanged();
                StopJobCommand.RaiseCanExecuteChanged();
                DeleteJobCommand.RaiseCanExecuteChanged();
            }
        }

        public ChartViewModel SelectedChart
        {
            get => _selectedChart;
            set
            {
                Set(ref _selectedChart, value);
            }
        }

        internal async Task StartTaskAsync()
        {
            ClearRunData();
            DataToModel();

            // Get account
            AccountModel account = null;
            if (!string.IsNullOrEmpty(Model.Account))
            {
                IReadOnlyList<AccountModel> accounts = null;
                var message = new NotificationMessageAction<List<AccountModel>>(Model.Account, m => accounts = m);
                Messenger.Default.Send(message);
                Debug.Assert(accounts != null);
                account = accounts.FirstOrDefault();
            }

            StrategyJobModel model = Model;
            try
            {
                _leanEngine = new Isolated<LeanEngine>();
                _cancel = new CancellationTokenSource();
                await Task.Run(() => model = _leanEngine.Value.Run(Model, account, new HostDomainLogger()), _cancel.Token);
                _leanEngine.Dispose();
                Model.Completed = true;
            }
            catch (AppDomainUnloadedException)
            {
                Log.Trace($"Strategy {Model.Name} canceled by user");
            }
            catch (Exception ex)
            {
                Log.Trace($"{ex.GetType()}: {ex.Message}");
                _leanEngine.Dispose();
            }

            // Update view
            Model = null;
            Model = model;

            DataFromModel();
            _cancel = null;
            _leanEngine = null;
            Enabled = false;
        }

        private async void OnEnableCommand(bool value)
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

        private async void OnStartJobCommand()
        {
            Enabled = true;
            await StartTaskAsync();
        }

        private void OnStopJobCommand(bool v)
        {
            StopTask();
            Enabled = false;
        }

        private void StopTask()
        {
            if (_cancel != null)
            {
                _cancel.Cancel();
            }

            if (_leanEngine != null)
            {
                _leanEngine.Dispose();
                _leanEngine = null;
            }
        }

        private void DeleteJob()
        {
            SelectedChart = null;
            Charts.Clear();
            _cancel?.Cancel();
            _parent?.DeleteJob(this);
        }

        internal void DataToModel()
        {
            Model.DataFolder = _settingsModel.DataFolder;

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

            SelectedChart = null;
            Charts.Clear();

            if (Model.Result != null)
            {
                // Allow proper decoding of orders.
                var result = JsonConvert.DeserializeObject<BacktestResult>(Model.Result, new[] { new OrderJsonConverter() });
                if (result != null)
                {
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

                    try
                    {
                        ParseCharts(result.Charts.MapToChartDefinitionDictionary());
                    }
                    catch (Exception ex)
                    {
                        Log.Trace($"Strategy {Model.Name} {ex.GetType()}: {ex.Message}");
                    }
                }
            }

            RaisePropertyChanged(() => Logs);
            RaisePropertyChanged(() => Loglines);
        }

        private void ClearRunData()
        {
            Model.Completed = false;
            Model.Logs = null;
            Model.Result = null;

            SelectedChart = null;
            Charts.Clear();

            Statistics.Clear();
            Orders.Clear();

            RaisePropertyChanged(() => Logs);
            RaisePropertyChanged(() => Loglines);
        }

        private void ParseCharts(Dictionary<string, ChartDefinition> charts)
        {
            var chartParser = new SeriesChartComponent();

            try
            {
                foreach (var chart in charts)
                {
                    foreach (var series in chart.Value.Series)
                    {
                        InstantChartPoint first = series.Value.Values.FirstOrDefault();
                        InstantChartPoint last = series.Value.Values.LastOrDefault();

                        LiveCharts.Wpf.Series chartSeries = chartParser.BuildSeries(series.Value);
                        chartParser.UpdateSeries(chartSeries, series.Value);

                        LiveCharts.Wpf.Series scrollSeries = chartParser.BuildSeries(series.Value);
                        chartParser.UpdateSeries(scrollSeries, series.Value);

                        Resolution resolution = SeriesChartComponent.DetectResolution(series.Value);
                        var viewModel = new ChartViewModel(chartSeries.Title, chartSeries, scrollSeries, first.X, last.X, resolution);
                        Charts.Add(viewModel);
                    }
                }

                SelectedChart = Charts.FirstOrDefault();
            }
            catch (Exception e)
            {
                Log.Error($"{e.GetType()}: {e.Message}");
            }
        }
    }
}
