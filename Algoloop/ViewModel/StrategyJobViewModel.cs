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
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Algoloop.ViewModel
{
    public class StrategyJobViewModel: ViewModelBase, IComparable
    {
        private StrategyViewModel _parent;
        private readonly SettingsModel _settingsModel;
        private CancellationTokenSource _cancel;
        private Isolated<LeanEngine> _leanEngine;
        private StrategyJobModel _model;
        private ChartViewModel _selectedChart;
        private bool _isSelected;
        private bool _isExpanded;

        public StrategyJobViewModel(StrategyViewModel parent, StrategyJobModel model, SettingsModel settingsModel)
        {
            _parent = parent;
            Model = model;
            _settingsModel = settingsModel;

            StartJobCommand = new RelayCommand(() => OnStartJobCommand(), () => !Active);
            StopJobCommand = new RelayCommand(() => OnStopJobCommand(false), () => Active);
            DeleteJobCommand = new RelayCommand(() => DeleteJob(), () => !Active);
            UseParametersCommand = new RelayCommand(() => UseParameters(), () => !Active);

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
        public RelayCommand UseParametersCommand { get; }

        public RelayCommand ActiveCommand { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => Set(ref _isExpanded, value);
        }

        public string Logs => Model.Logs;

        public int Loglines => Logs == null ? 0 : Logs.Count(m => m.Equals('\n'));

        public bool Active
        {
            get => Model.Active;
            set
            {
                Model.Active = value;
                RaisePropertyChanged(() => Active);
                StartJobCommand.RaiseCanExecuteChanged();
                StopJobCommand.RaiseCanExecuteChanged();
                DeleteJobCommand.RaiseCanExecuteChanged();
                if (value)
                {
                    StartTaskAsync();
                }
                else
                {
                    StopTask();
                }
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

        public override string ToString()
        {
            return _model.Name;
        }

        public void DeleteJob()
        {
            SelectedChart = null;
            Charts.Clear();
            _cancel?.Cancel();
            _parent?.DeleteJob(this);
        }

        internal async Task StartTaskAsync()
        {
            ClearRunData();
            DataToModel();
            DataRow row = _parent.CreateSummaryRow(this);
            _parent.RefreshSummary();

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

            // Set search path if not base directory
            string folder = Path.GetDirectoryName(Model.AlgorithmLocation);
            if (!AppDomain.CurrentDomain.BaseDirectory.Equals(folder))
            {
                StrategyViewModel.AddPath(folder);
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

            Active = false;
            DataFromModel();
            _cancel = null;
            _leanEngine = null;
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
            DataRow row = _parent.CreateSummaryRow(this);
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
                if (parameterModel.UseValue)
                {
                    _parent.Summary.Add(row, parameterModel.Name, parameterModel.Value);
                }
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
                    AddCustomStatistics(result, row);

                    foreach (var item in result.Statistics)
                    {
                        var statisticViewModel = new StatisticViewModel { Name = item.Key, Value = item.Value };
                        Statistics.Add(statisticViewModel);
                        _parent.Summary.Add(row, item.Key, item.Value);
                    }

                    foreach (var item in result.RuntimeStatistics)
                    {
                        var statisticViewModel = new StatisticViewModel { Name = item.Key, Value = item.Value };
                        Statistics.Add(statisticViewModel);
                        _parent.Summary.Add(row, item.Key, item.Value);
                    }

                    foreach (var order in result.Orders.OrderBy(o => o.Key))
                    {
                        Orders.Add(order.Value);
                    }

                    try
                    {
                        ParseCharts(result.Charts);
                    }
                    catch (Exception ex)
                    {
                        Log.Trace($"Strategy {Model.Name} {ex.GetType()}: {ex.Message}");
                    }
                }
            }

            _parent.RefreshSummary();
            RaisePropertyChanged(() => Logs);
            RaisePropertyChanged(() => Loglines);
        }

        private void AddCustomStatistics(BacktestResult result, DataRow row)
        {
            string profit = result.Statistics["Net Profit"];
            string dd = result.Statistics["Drawdown"];
            bool isNetProfit = decimal.TryParse(profit.Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal netProfit)
                && profit.Contains("%");
            bool isDrawdown = decimal.TryParse(dd.Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal drawdown)
                && dd.Contains("%");

            if (isNetProfit && isDrawdown && drawdown != 0)
            {
                string ratio = (netProfit / drawdown).RoundToSignificantDigits(4).ToString(CultureInfo.InvariantCulture);
                var statisticViewModel = new StatisticViewModel { Name = "Profit-DD", Value = ratio};
                Statistics.Add(statisticViewModel);
                _parent.Summary.Add(row, "Profit-DD", ratio);
            }
        }

        private void UseParameters()
        {
            _parent?.UseParameters(this);
        }

        private async void OnStartJobCommand()
        {
            Active = true;
            await StartTaskAsync();
        }

        private void OnStopJobCommand(bool v)
        {
            StopTask();
            Active = false;
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

        private void ParseCharts(IDictionary<string, Chart> charts)
        {
            try
            {
                foreach (var chart in charts)
                {
                    foreach (var serie in chart.Value.Series)
                    {
                        var viewModel = new ChartViewModel(serie.Value);
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

        public int CompareTo(object obj)
        {
            var a = obj as StrategyJobViewModel;
            return string.Compare(Model.Name, a?.Model.Name);
        }
    }
}
