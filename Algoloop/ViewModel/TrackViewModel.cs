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
using Algoloop.Properties;
using Algoloop.Service;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Statistics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Algoloop.ViewModel
{
    public class TrackViewModel: ViewModelBase, ITreeViewModel, IComparable
    {
        private StrategyViewModel _parent;
        private readonly MarketService _markets;
        private readonly AccountService _accounts;
        private readonly SettingService _settings;
        private static object mutex = new object();

        private CancellationTokenSource _cancel;
        private Isolated<LeanLauncher> _leanEngine;
        private TrackModel _model;
        private bool _isSelected;
        private bool _isExpanded;
        private SyncObservableCollection<ChartViewModel> _charts = new SyncObservableCollection<ChartViewModel>();
        public IDictionary<string, decimal?> _statistics;
        private string _port;
        private IList _selectedItems;
        private SyncObservableCollection<SymbolViewModel> _symbols = new SyncObservableCollection<SymbolViewModel>();
        private SyncObservableCollection<ParameterViewModel> _parameters = new SyncObservableCollection<ParameterViewModel>();
        private SyncObservableCollection<Trade> _trades = new SyncObservableCollection<Trade>();
        private SyncObservableCollection<SymbolSummaryViewModel> _summarySymbols = new SyncObservableCollection<SymbolSummaryViewModel>();
        private SyncObservableCollection<OrderViewModel> _orders = new SyncObservableCollection<OrderViewModel>();
        private SyncObservableCollection<HoldingViewModel> _holdings = new SyncObservableCollection<HoldingViewModel>();
        private bool _loaded;
        private string _logs;

        public TrackViewModel(StrategyViewModel parent, TrackModel model, MarketService markets, AccountService accounts, SettingService settings)
        {
            _parent = parent;
            Model = model;
            _markets = markets;
            _accounts = accounts;
            _settings = settings;

            StartCommand = new RelayCommand(() => DoStartTaskCommand(), () => !Active);
            StopCommand = new RelayCommand(() => DoStopTaskCommand(false), () => Active);
            DeleteCommand = new RelayCommand(() => DoDeleteTrack(), () => !Active);
            UseParametersCommand = new RelayCommand(() => DoUseParameters(), () => !Active);
            ExportSymbolsCommand = new RelayCommand<IList>(m => DoExportSymbols(m), m => true);
            CloneStrategyCommand = new RelayCommand<IList>(m => DoCloneStrategy(m), m => true);
            CreateFolderCommand = new RelayCommand<IList>(m => DoCreateFolder(m), m => true);
            ExportCommand = new RelayCommand(() => { }, () => false);
            CloneCommand = new RelayCommand(() => DoCloneStrategy(null), () => true);
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
        public RelayCommand<IList> ExportSymbolsCommand { get; }
        public RelayCommand<IList> CloneStrategyCommand { get; }
        public RelayCommand<IList> CreateFolderCommand { get; }

        public IList SelectedItems
        {
            get { return _selectedItems; }
            set
            {
                _selectedItems = value;
                string message = string.Empty;
                if (_selectedItems?.Count > 0)
                {
                    message = string.Format(Resources.SelectedCount, _selectedItems.Count);
                }

                Messenger.Default.Send(new NotificationMessage(message));
            }
        }

        public IDictionary<string, decimal?> Statistics
        {
            get => _statistics;
            set => Set(ref _statistics, value);
        }

        public TrackModel Model
        {
            get => _model;
            set => Set(ref _model, value);
        }

        public string Logs
        {
            get => _logs;
            set
            {
                Set(ref _logs, value);
                RaisePropertyChanged(() => Loglines);
            }
        }

        public int Loglines => Logs == null ? 0 : Logs.Count(m => m.Equals('\n'));

        public SyncObservableCollection<SymbolViewModel> Symbols
        {
            get
            {
                LoadTrack();
                return _symbols;
            }
            set => Set(ref _symbols, value);
        }

        public SyncObservableCollection<ParameterViewModel> Parameters
        {
            get
            {
                LoadTrack();
                return _parameters;
            }
            set => Set(ref _parameters, value);        
        }

        public SyncObservableCollection<Trade> Trades
        {
            get
            {
                LoadTrack();
                return _trades;
            }
            set => Set(ref _trades, value);
        }

        public SyncObservableCollection<SymbolSummaryViewModel> SummarySymbols
        {
            get
            {
                LoadTrack();
                return _summarySymbols;
            }
            set => Set(ref _summarySymbols, value);
        }

        public SyncObservableCollection<OrderViewModel> Orders
        {
            get
            {
                LoadTrack();
                return _orders;
            }
            set => Set(ref _orders, value);
        }

        public SyncObservableCollection<HoldingViewModel> Holdings
        {
            get
            {
                LoadTrack();
                return _holdings;
            }
            set => Set(ref _holdings, value);
        }

        public SyncObservableCollection<ChartViewModel> Charts
        {
            get
            {
                LoadTrack();
                return _charts;
            }
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

        public void DoDeleteTrack()
        {
            if (File.Exists(Model.Result))
            {
                File.Delete(Model.Result);
            }

            if (File.Exists(Model.Logs))
            {
                File.Delete(Model.Logs);
            }

            var charts = Charts;
            charts.Clear();
            Charts = null;
            Charts = charts;

            _cancel?.Cancel();
            _parent?.DeleteTrack(this);
        }

        public int CompareTo(object obj)
        {
            var a = obj as TrackViewModel;
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

            // Find account
            AccountModel account = _accounts.FindAccount(Model.Account);

            // Set search path if not base directory
            string folder = Path.GetDirectoryName(Model.AlgorithmLocation);
            if (!AppDomain.CurrentDomain.BaseDirectory.Equals(folder))
            {
                StrategyViewModel.AddPath(folder);
            }

            TrackModel model = Model;
            try
            {
                if (Desktop)
                {
                    Port = _settings.DesktopPort > 0 ? _settings.DesktopPort.ToString() : null;
                    _leanEngine = new Isolated<LeanLauncher>();
                    _cancel = new CancellationTokenSource();
                    await Task.Run(() => model = _leanEngine.Value.Run(Model, account, _settings, new HostDomainLogger()), _cancel.Token);
                    Port = null;
                }
                else
                {
                    _leanEngine = new Isolated<LeanLauncher>();
                    _cancel = new CancellationTokenSource();
                    await Task.Run(() => model = _leanEngine.Value.Run(Model, account, _settings, new HostDomainLogger()), _cancel.Token);
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

            // Split result and logs to separate files
            lock (mutex)
            {
                SplitModelToFiles(model);
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
        }

        internal void DataFromModel()
        {
            // Cleanup
            _symbols.Clear();
            _parameters.Clear();
            _trades.Clear();
            _orders.Clear();
            _holdings.Clear();
            var charts = _charts;
            charts.Clear();
            _charts = null;
            Charts = charts;
            RaisePropertyChanged(() => Logs);
            RaisePropertyChanged(() => Loglines);

            // Statistics results
            Statistics = Model.Statistics == null 
                ? new SafeDictionary<string, decimal?>() 
                : new SafeDictionary<string, decimal?>(Model.Statistics);
        }

        private void LoadTrack()
        {
            if (_loaded)
                return;

            _loaded = true;

            // Get symbols from model
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                var symbolViewModel = new SymbolViewModel(null, symbolModel);
                _symbols.Add(symbolViewModel);
            }

            // Get parameters from model
            foreach (ParameterModel parameterModel in Model.Parameters)
            {
                var parameterViewModel = new ParameterViewModel(null, parameterModel);
                _parameters.Add(parameterViewModel);
            }

            // Read Logs
            if (File.Exists(Model.Logs))
            {
                using (StreamReader r = new StreamReader(Model.Logs))
                {
                    _logs = r.ReadToEnd();
                }
            }
            else
            {
                _logs = string.Empty;
            }

            // Read results
            BacktestResult result = null;
            if (File.Exists(Model.Logs))
            {
                using (StreamReader r = new StreamReader(Model.Result))
                {
                    using (JsonReader reader = new JsonTextReader(r))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Converters.Add(new OrderJsonConverter());
                        result = serializer.Deserialize<BacktestResult>(reader);
                    }
                }
            }

            // Process results
            if (result == null)
                return;

            // Closed trades result
            result.TotalPerformance.ClosedTrades.ForEach(m => _trades.Add(m));
            foreach (Trade trade in _trades)
            {
                SymbolSummaryViewModel summary = _summarySymbols.FirstOrDefault(m => m.Symbol.Equals(trade.Symbol.Value));
                if (summary == null)
                {
                    summary = new SymbolSummaryViewModel(trade.Symbol);
                    _summarySymbols.Add(summary);
                }

                summary.AddTrade(trade);
            }

            _summarySymbols.ToList().ForEach(m => m.Calculate());

            // Orders result
            foreach (var pair in result.Orders.OrderBy(o => o.Key))
            {
                Order order = pair.Value;
                _orders.Add(new OrderViewModel(order));
                if (order.Status.Equals(OrderStatus.Submitted)
                    || order.Status.Equals(OrderStatus.Canceled)
                    || order.Status.Equals(OrderStatus.CancelPending)
                    || order.Status.Equals(OrderStatus.None)
                    || order.Status.Equals(OrderStatus.New)
                    || order.Status.Equals(OrderStatus.Invalid))
                    continue;

                HoldingViewModel holding = _holdings.FirstOrDefault(m => m.Symbol.Equals(order.Symbol));
                if (holding == null)
                {
                    holding = new HoldingViewModel(order.Symbol)
                    {
                        Price = order.Price,
                        Quantity = order.Quantity,
                        Profit = order.Value,
                        Duration = (order.LastUpdateTime ?? Model.EndDate) - order.CreatedTime
                    };

                    _holdings.Add(holding);
                }
                else
                {
                    decimal quantity = holding.Quantity + order.Quantity;
                    holding.Price = quantity == 0 ? 0 : (holding.Price * holding.Quantity + order.Price * order.Quantity) / quantity;
                    holding.Quantity += order.Quantity;
                    holding.Profit += order.Value;
                    if (holding.Quantity == 0)
                    {
                        _holdings.Remove(holding);
                    }
                }
            }

            // Charts results
            try
            {
                ParseCharts(result);
            }
            catch (Exception ex)
            {
                Log.Trace($"Strategy {Model.Name} {ex.GetType()}: {ex.Message}");
            }
        }

        private void AddStatisticItem(IDictionary<string, decimal?> statistics, string name, string text)
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
        }

        private void AddCustomStatistics(IDictionary<string, decimal?> statistics, BacktestResult result)
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

        private void SplitModelToFiles(TrackModel model)
        {
            // Create folder for track files
            string folder = "Tracks";
            Directory.CreateDirectory(folder);

            // Save result
            string resultFileTemplate = Path.Combine(folder, "result.json");
            string resultFile = UniqueFileName(resultFileTemplate);
            using (StreamWriter file = File.CreateText(resultFile))
            {
                file.Write(model.Result);
            }

            // Save logs
            string logFileTemplate = Path.Combine(folder, "log.log");
            string logFile = UniqueFileName(logFileTemplate);
            using (StreamWriter file = File.CreateText(logFile))
            {
                file.Write(model.Logs);
            }

            // Process results
            BacktestResult result = JsonConvert.DeserializeObject<BacktestResult>(model.Result, new[] { new OrderJsonConverter() });
            if (result != null)
            {
                IDictionary<string, decimal?> statistics = new SafeDictionary<string, decimal?>();
                AddCustomStatistics(statistics, result);
                foreach (KeyValuePair<string, string> item in result.Statistics)
                {
                    AddStatisticItem(statistics, item.Key, item.Value);
                }

                foreach (KeyValuePair<string, string> item in result.RuntimeStatistics)
                {
                    AddStatisticItem(statistics, item.Key, item.Value);
                }

                model.Statistics = statistics;
            }

            // Replace results and logs with file references
            model.Result = resultFile;
            model.Logs = logFile;
        }

        private static string UniqueFileName(string path)
        {
            int count = 0;
            string candidate;

            do
            {
                count++;
                candidate = string.Format(
                    @"{0}\{1}{2}{3}",
                    Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path),
                    count,
                    Path.GetExtension(path));

            } while (File.Exists(candidate));

            return candidate;
        }

        private void DoUseParameters()
        {
            _parent?.UseParameters(this);
        }

        private async void DoStartTaskCommand()
        {
            Active = true;
            await StartTaskAsync();
        }

        private void DoStopTaskCommand(bool v)
        {
            StopTask();
            Active = false;
        }

        private void DoExportSymbols(IList symbols)
        {
            Debug.Assert(symbols != null);
            if (symbols.Count == 0)
                return;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = "symbol file (*.csv)|*.csv|All files (*.*)|*.*"
            };
            if (saveFileDialog.ShowDialog() == false)
                return;

            try
            {
                string fileName = saveFileDialog.FileName;
                using (StreamWriter file = File.CreateText(fileName))
                {
                    foreach (SymbolSummaryViewModel symbol in symbols)
                    {
                        file.WriteLine(symbol.Symbol);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
            }
        }

        private void DoCloneStrategy(IList symbols)
        {
            var strategyModel = new StrategyModel(Model);
            if (symbols != null)
            {
                strategyModel.Symbols.Clear();
                IEnumerable<SymbolModel> symbolModels = symbols.Cast<SymbolSummaryViewModel>().Select(m => new SymbolModel(m.Symbol));
                strategyModel.Symbols.AddRange(symbolModels);
            }

            _parent.CloneStrategy(strategyModel);
        }

        private void DoCreateFolder(IList list)
        {
            if (list == null)
                return;

            IEnumerable<string> symbols = list.Cast<SymbolSummaryViewModel>().Select(m => m.Symbol);
            _parent.CreateFolder(symbols);
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
            Model.Statistics = null;

            var charts = _charts;
            charts.Clear();
            Charts = null;
            Charts = charts;

            Logs = null;

            _orders.Clear();
            _holdings.Clear();
            _trades.Clear();
            _summarySymbols.Clear();

            _statistics.Clear();
            IDictionary<string, decimal?> statistics = _statistics;
            Statistics = null;
            Statistics = statistics;

            _loaded = false;
        }

        private void ParseCharts(Result result)
        {
            SyncObservableCollection<ChartViewModel> workCharts = _charts;
            Debug.Assert(workCharts.Count == 0);

            var series = new Series("Net profit", SeriesType.Line, "$", Color.Green, ScatterMarkerSymbol.Diamond);
            decimal profit = Model.InitialCapital;
            series.AddPoint(Model.StartDate, profit);
            foreach (KeyValuePair<DateTime, decimal> trade in result.ProfitLoss)
            {
                profit += trade.Value;
                series.AddPoint(trade.Key, profit);
            }

            workCharts.Add(new ChartViewModel(series));

            foreach (KeyValuePair<string, Chart> chart in result.Charts)
            {
                foreach (KeyValuePair<string, Series> serie in chart.Value.Series)
                {
                    if (serie.Value.Values.Count < 2)
                        continue;

                    workCharts.Add(new ChartViewModel(serie.Value));
                }
            }

            Charts = null;
            Charts = workCharts;
        }
    }
}
