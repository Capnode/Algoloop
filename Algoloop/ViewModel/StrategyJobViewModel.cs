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

using Algoloop.Common;
using Algoloop.Lean;
using Algoloop.Model;
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
    public class StrategyJobViewModel: ViewModelBase, ITreeViewModel, IComparable
    {
        private StrategyViewModel _parent;
        private readonly SettingsModel _settingsModel;
        private CancellationTokenSource _cancel;
        private Isolated<LeanLauncher> _leanEngine;
        private StrategyJobModel _model;
        private bool _isSelected;
        private bool _isExpanded;
        private SyncObservableCollection<ChartViewModel> _charts = new SyncObservableCollection<ChartViewModel>();
        public IDictionary<string, object> _statistics;
        private string _port;

        public StrategyJobViewModel(StrategyViewModel parent, StrategyJobModel model, SettingsModel settingsModel)
        {
            _parent = parent;
            Model = model;
            _settingsModel = settingsModel;

            StartCommand = new RelayCommand(() => DoStartJobCommand(), () => !Active);
            StopCommand = new RelayCommand(() => DoStopJobCommand(false), () => Active);
            DeleteCommand = new RelayCommand(() => DoDeleteJob(), () => !Active);
            UseParametersCommand = new RelayCommand(() => DoUseParameters(), () => !Active);
            ExportCommand = new RelayCommand(() => { }, () => false);
            CloneCommand = new RelayCommand(() => { }, () => false);
            CloneAlgorithmCommand = new RelayCommand(() => { }, () => false);

            DataFromModel();
        }

        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand UseParametersCommand { get; }
        public RelayCommand ActiveCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand CloneCommand { get; }
        public RelayCommand CloneAlgorithmCommand { get; }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();
        public SyncObservableCollection<ParameterViewModel> Parameters { get; } = new SyncObservableCollection<ParameterViewModel>();
        public SyncObservableCollection<Order> Orders { get; } = new SyncObservableCollection<Order>();
        public SyncObservableCollection<HoldingViewModel> Holdings { get; } = new SyncObservableCollection<HoldingViewModel>();

        public IDictionary<string, object> Statistics
        {
            get => _statistics;
            set => Set(ref _statistics, value);
        }

        public StrategyJobModel Model
        {
            get => _model;
            set => Set(ref _model, value);
        }

        public SyncObservableCollection<ChartViewModel> Charts
        {
            get => _charts;
            set => Set(ref _charts, value);
        }

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
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                if (value)
                {
                    Task task = StartTaskAsync();
                }
                else
                {
                    StopTask();
                }
            }
        }

        public string Port
        {
            get => _port;
            set => Set(ref _port, value);
        }

        public bool Desktop
        {
            get => Model.Desktop;
            set
            {
                Model.Desktop = value;
                RaisePropertyChanged();
            }
        }

        public override string ToString()
        {
            return _model.Name;
        }

        public void DoDeleteJob()
        {
            var charts = Charts;
            charts.Clear();
            Charts = null;
            Charts = charts;

            _cancel?.Cancel();
            _parent?.DeleteJob(this);
        }

        public int CompareTo(object obj)
        {
            var a = obj as StrategyJobViewModel;
            return string.Compare(Model.Name, a?.Model.Name);
        }

        public void Refresh()
        {
            Model.Refresh();
        }

        internal async Task StartTaskAsync()
        {
            ClearRunData();
            DataToModel();

            // Account must not be null
            if (Model.Account == null)
            {
                Active = false;
                Log.Error($"Strategy {Model.Name}: Account is not defined!");
                return;
            }

            // Get account
            IReadOnlyList<AccountModel> accounts = null;
            var message = new NotificationMessageAction<List<AccountModel>>(Model.Account, m => accounts = m);
            Messenger.Default.Send(message);
            AccountModel account = accounts?.FirstOrDefault();

            // Set search path if not base directory
            string folder = Path.GetDirectoryName(Model.AlgorithmLocation);
            if (!AppDomain.CurrentDomain.BaseDirectory.Equals(folder))
            {
                StrategyViewModel.AddPath(folder);
            }

            StrategyJobModel model = Model;
            try
            {
                if (Desktop)
                {
                    Port = _settingsModel.DesktopPort > 0 ? _settingsModel.DesktopPort.ToString() : null;
                    _leanEngine = new Isolated<LeanLauncher>();
                    _cancel = new CancellationTokenSource();
                    await Task.Run(() => model = _leanEngine.Value.Run(Model, account, _settingsModel, new HostDomainLogger()), _cancel.Token);
                    Port = null;
                }
                else
                {
                    _leanEngine = new Isolated<LeanLauncher>();
                    _cancel = new CancellationTokenSource();
                    await Task.Run(() => model = _leanEngine.Value.Run(Model, account, _settingsModel, new HostDomainLogger()), _cancel.Token);
                }

                _leanEngine.Dispose();
                model.Completed = true;
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
                if (parameterModel.UseValue)
                {
//                    _parent.Summary.Add(row, parameterModel.Name, parameterModel.Value);
                }
            }

            Orders.Clear();
            Holdings.Clear();
            var charts = Charts;
            charts.Clear();
            Charts = null;
            Charts = charts;
            IDictionary<string, object> statistics = new SafeDictionary<string, object>();

            if (Model.Result != null)
            {
                // Allow proper decoding of orders.
                var result = JsonConvert.DeserializeObject<BacktestResult>(Model.Result, new[] { new OrderJsonConverter() });
                if (result != null)
                {
                    AddCustomStatistics(statistics, result);

                    foreach (KeyValuePair<string, string> item in result.Statistics)
                    {
                        AddStatisticItem(statistics, item.Key, item.Value);
                    }

                    foreach (KeyValuePair<string, string> item in result.RuntimeStatistics)
                    {
                        AddStatisticItem(statistics, item.Key, item.Value);
                    }

                    Statistics = statistics;

                    // Process orders
                    foreach (var pair in result.Orders.OrderBy(o => o.Key))
                    {
                        Order order = pair.Value;
                        Orders.Add(order);
                        if (order.Status.Equals(OrderStatus.Submitted))
                            continue;

                        HoldingViewModel holding = Holdings.FirstOrDefault(m => m.Symbol.Equals(order.Symbol));
                        if (holding == null)
                        {
                            holding = new HoldingViewModel(order.Symbol)
                            {
                                Price = order.Price,
                                Quantity = order.Quantity,
                                Profit = order.Value
                            };

                            Holdings.Add(holding);
                        }
                        else
                        {
                            decimal quantity = holding.Quantity + order.Quantity;
                            holding.Price = quantity == 0 ? 0 : (holding.Price * holding.Quantity + order.Price * order.Quantity) / quantity;
                            holding.Quantity += order.Quantity;
                            holding.Profit += order.Value;
                            if (holding.Quantity == 0)
                            {
                                Holdings.Remove(holding);
                            }
                        }
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

            RaisePropertyChanged(() => Logs);
            RaisePropertyChanged(() => Loglines);
        }

        private void AddStatisticItem(IDictionary<string, object> statistics, string name, string text)
        {
            decimal value;
            if (text.Contains("$") && decimal.TryParse(text.Replace("$", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                string header = name + "$";
                statistics.Add(header, value);
            }
            else if (text.Contains("%") && decimal.TryParse(text.Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                string header = name + "%";
                statistics.Add(header, value);
            }
            else if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                statistics.Add(name, value);
            }
            else if (bool.TryParse(text, out bool boolVal))
            {
                statistics.Add(name, boolVal);
            }
            else
            {
                statistics.Add(name, text);
            }
        }

        private void AddCustomStatistics(IDictionary<string, object> statistics, BacktestResult result)
        {
            if (result.Statistics.Count == 0)
                return;

            string profit = result.Statistics["Net Profit"];
            string dd = result.Statistics["Drawdown"];
            bool isNetProfit = decimal.TryParse(profit.Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal netProfit)
                && profit.Contains("%");
            bool isDrawdown = decimal.TryParse(dd.Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal drawdown)
                && dd.Contains("%");

            if (isNetProfit && isDrawdown && drawdown != 0)
            {
                decimal ratio = (netProfit / drawdown).RoundToSignificantDigits(4);
                statistics.Add("Profit-DD", ratio);
            }
        }

        private void DoUseParameters()
        {
            _parent?.UseParameters(this);
        }

        private async void DoStartJobCommand()
        {
            Active = true;
            await StartTaskAsync();
        }

        private void DoStopJobCommand(bool v)
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

            var charts = Charts;
            charts.Clear();
            Charts = null;
            Charts = charts;

            Statistics = null;
            Orders.Clear();
            Holdings.Clear();

            RaisePropertyChanged(() => Logs);
            RaisePropertyChanged(() => Loglines);
        }

        private void ParseCharts(IDictionary<string, Chart> charts)
        {
            var workCharts = Charts;
            Debug.Assert(workCharts.Count == 0);
            try
            {
                foreach (KeyValuePair<string, Chart> chart in charts)
                {
                    foreach (KeyValuePair<string, Series> serie in chart.Value.Series)
                    {
                        if (serie.Value.Values.Count < 2)
                            continue;
                        var viewModel = new ChartViewModel(serie.Value);
                        workCharts.Add(viewModel);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.GetType()}: {e.Message}");
            }

            Charts = null;
            Charts = workCharts;
        }
    }
}
