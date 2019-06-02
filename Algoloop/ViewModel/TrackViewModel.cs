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
using Ionic.Zip;
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Algoloop.ViewModel
{
    public class TrackViewModel: ViewModelBase, ITreeViewModel, IComparable
    {
        public const string Folder = "Tracks";
        private const string _logFile = "Logs.log";
        private const string _resultFile = "Result.json";
        private const string _zipFile = "track.zip";
        private const double _daysInYear = 365.24;

        private StrategyViewModel _parent;
        private readonly MarketService _markets;
        private readonly AccountService _accounts;
        private readonly SettingService _settings;
        private static object mutex = new object();

        private CancellationTokenSource _cancel;
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
        private SyncObservableCollection<TrackSymbolViewModel> _trackSymbols = new SyncObservableCollection<TrackSymbolViewModel>();
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
            get => _symbols;
            set => Set(ref _symbols, value);
        }

        public SyncObservableCollection<ParameterViewModel> Parameters
        {
            get => _parameters;
            set => Set(ref _parameters, value);        
        }

        public SyncObservableCollection<Trade> Trades
        {
            get => _trades;
            set => Set(ref _trades, value);
        }

        public SyncObservableCollection<TrackSymbolViewModel> TrackSymbols
        {
            get => _trackSymbols;
            set => Set(ref _trackSymbols, value);
        }

        public SyncObservableCollection<OrderViewModel> Orders
        {
            get => _orders;
            set => Set(ref _orders, value);
        }

        public SyncObservableCollection<HoldingViewModel> Holdings
        {
            get => _holdings;
            set => Set(ref _holdings, value);
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
            return Model.Name;
        }

        public void DoDeleteTrack()
        {
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
            if (!_loaded && Model.Completed)
            {
                LoadTrack();
                _loaded = true;
            }
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
                if (Desktop && _settings.DesktopPort > 0)
                {
                    Port = _settings.DesktopPort.ToString();
                    model = await RunTrack(account, model);
                    Port = null;
                }
                else
                {
                    model = await RunTrack(account, model);
                }

                model.Completed = true;

                // Split result and logs to separate files
                SplitModelToFiles(model);
            }
            catch (AppDomainUnloadedException)
            {
                Log.Trace($"Strategy {Model.Name} canceled by user");
            }
            catch (Exception ex)
            {
                Log.Trace($"{ex.GetType()}: {ex.Message}");
            }

            // Update view
            Model = null;
            Model = model;

            Active = false;
            DataFromModel();
        }

        internal void DataToModel()
        {
        }

        internal void DataFromModel()
        {
            // Cleanup
            Symbols.Clear();
            Parameters.Clear();
            Trades.Clear();
            Orders.Clear();
            Holdings.Clear();

            var charts = Charts;
            charts.Clear();
            Charts = null;
            Charts = charts;

            Logs = string.Empty;

            // Statistics results
            Statistics = Model.Statistics == null 
                ? new SafeDictionary<string, decimal?>() 
                : new SafeDictionary<string, decimal?>(Model.Statistics);

            if (!_loaded && IsSelected)
            {
                LoadTrack();
                _loaded = true;
            }
        }

        internal static double CalculateScore(IList<Trade> trades, out double expectancy)
        {
            expectancy = 0;
            if (trades == null || !trades.Any())
            {
                return 0;
            }

            double score = 0;
            Debug.Assert(!trades.Min(m => m.EntryTime).Equals(DateTime.FromBinary(0)));
            Debug.Assert(!trades.Min(m => m.ExitTime).Equals(DateTime.FromBinary(0)));
            double profitLoss = (double)trades.Sum(m => m.ProfitLoss);
            int count = trades.Count();
            double risk = -(double)trades.Min(m => m.MAE);
            DateTime first = trades.Min(m => m.EntryTime);
            DateTime last = trades.Max(m => m.ExitTime);
            TimeSpan duration = last - first;
            double years = duration.Ticks / (_daysInYear * TimeSpan.TicksPerDay);
            if (years > 0 && count > 0 && risk > 0)
            {
                expectancy = profitLoss / count / risk;
                score = (count / years) * expectancy;
                score = Scale(score);
            }

            return score;
        }

        private void AddCustomStatistics(IDictionary<string, decimal?> statistics, BacktestResult result)
        {
            double score = CalculateScore(Trades, out double expectancy);
            score = score.RoundToSignificantDigits(4);
            expectancy = expectancy.RoundToSignificantDigits(4);
            statistics.Add("Score", (decimal)score);
            statistics.Add("Expectancy", (decimal)expectancy);
        }

        private static double Scale(double x)
        {
            return x / Math.Sqrt(1 + x * x);
        }

        private static double ScaleToRange(double x, double minimum, double maximum)
        {
            return (x - minimum) / (maximum - minimum);
        }

        private async Task<TrackModel> RunTrack(AccountModel account, TrackModel model)
        {
            try
            {
                using (Isolated<LeanLauncher> leanEngine = new Isolated<LeanLauncher>())
                {
                    _cancel = new CancellationTokenSource();
                    await Task.Run(() => model = leanEngine.Value.Run(Model, account, _settings, new HostDomainLogger()), _cancel.Token);
                }
            }
            finally
            {
                _cancel = null;
            }

            return model;
        }

        private void LoadTrack()
        {
            // Get symbols from model
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                var symbolViewModel = new SymbolViewModel(null, symbolModel);
                Symbols.Add(symbolViewModel);
            }

            // Get parameters from model
            foreach (ParameterModel parameterModel in Model.Parameters)
            {
                var parameterViewModel = new ParameterViewModel(null, parameterModel);
                Parameters.Add(parameterViewModel);
            }

            // Find track zipfile
            if (!File.Exists(Model.ZipFile))
            {
                return;
            }

            // Unzip result file
            ZipFile zipFile;
            BacktestResult result = null;
            using (StreamReader resultStream = Compression.Unzip(Model.ZipFile, _resultFile, out zipFile))
            using (zipFile)
            {
                if (resultStream == null)
                {
                    return;
                }

                using (JsonReader reader = new JsonTextReader(resultStream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Converters.Add(new OrderJsonConverter());
                    result = serializer.Deserialize<BacktestResult>(reader);
                    if (result == null)
                    {
                        return;
                    }
                }
            }

            // Validate if statistics same
            IDictionary<string, decimal?> statistics = ReadStatistics(result);
            if (Model.Statistics.Count != statistics.Count
                || Model.Statistics.Except(statistics).Any())
            {
                return;
            }

            // Trade details
            foreach (Trade trade in Trades)
            {
                TrackSymbolViewModel trackSymbol = TrackSymbols.FirstOrDefault(m => m.Symbol.Equals(trade.Symbol.Value));
                if (trackSymbol == null)
                {
                    trackSymbol = new TrackSymbolViewModel(trade.Symbol);
                    TrackSymbols.Add(trackSymbol);
                }

                trackSymbol.AddTrade(trade);
            }

            TrackSymbols.ToList().ForEach(m => m.Calculate());

            // Orders result
            foreach (var pair in result.Orders.OrderBy(o => o.Key))
            {
                Order order = pair.Value;
                Orders.Add(new OrderViewModel(order));
                if (order.Status.Equals(OrderStatus.Submitted)
                    || order.Status.Equals(OrderStatus.Canceled)
                    || order.Status.Equals(OrderStatus.CancelPending)
                    || order.Status.Equals(OrderStatus.None)
                    || order.Status.Equals(OrderStatus.New)
                    || order.Status.Equals(OrderStatus.Invalid))
                    continue;

                HoldingViewModel holding = Holdings.FirstOrDefault(m => m.Symbol.Equals(order.Symbol));
                if (holding == null)
                {
                    holding = new HoldingViewModel(order.Symbol)
                    {
                        Price = order.Price,
                        Quantity = order.Quantity,
                        Profit = order.Value,
                        Duration = (order.LastUpdateTime ?? Model.EndDate) - order.CreatedTime
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

            // Charts results
            try
            {
                ParseCharts(result);
            }
            catch (Exception ex)
            {
                Log.Trace($"Strategy {Model.Name} {ex.GetType()}: {ex.Message}");
            }

            // Unzip log file
            using (StreamReader logStream = Compression.Unzip(Model.ZipFile, _logFile, out zipFile))
            using (zipFile)
            {
                if (logStream != null)
                {
                    Logs = logStream.ReadToEnd();
                }
            }
        }

        private void AddStatisticItem(IDictionary<string, decimal?> statistics, string name, string text)
        {
            // Make unique name
            while (statistics.TryGetValue(name, out _))
            {
                name += "+";
            }

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

        private void SplitModelToFiles(TrackModel model)
        {
            // Create folder for track files
            Directory.CreateDirectory(Folder);
            string zipFileTemplate = Path.Combine(Folder, _zipFile);

            // Save logs and result to zipfile
            lock (mutex)
            {
                string zipFile = UniqueFileName(zipFileTemplate);
                Compression.ZipData(zipFile, new Dictionary<string, string>
                {
                    { _logFile, model.Logs },
                    { _resultFile, model.Result }
                });

                model.ZipFile = zipFile;
            }

            // Process results
            BacktestResult result = JsonConvert.DeserializeObject<BacktestResult>(model.Result, new[] { new OrderJsonConverter() });
            if (result != null)
            {
                // Statistics
                model.Statistics = ReadStatistics(result);
                Trades.Clear();
            }

            // Replace results and logs with file references
            model.Result = string.Empty;
            model.Logs = string.Empty;
        }

        private IDictionary<string, decimal?> ReadStatistics(BacktestResult result)
        {
            // Closed trades result
            Trades.Clear();
            result.TotalPerformance.ClosedTrades.ForEach(m => Trades.Add(m));

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

            PortfolioStatistics portfolioStatistics = result.TotalPerformance.PortfolioStatistics;
            PropertyInfo[] portfolioProperties = typeof(PortfolioStatistics).GetProperties();
            foreach (PropertyInfo property in portfolioProperties)
            {
                object value = property.GetValue(portfolioStatistics);
                AddStatisticItem(statistics,property.Name, Convert.ToString(value, CultureInfo.InvariantCulture));
            }

            TradeStatistics tradeStatistics = result.TotalPerformance.TradeStatistics;
            PropertyInfo[] tradeProperties = typeof(TradeStatistics).GetProperties();
            foreach (PropertyInfo property in tradeProperties)
            {
                object value = property.GetValue(tradeStatistics);
                AddStatisticItem(statistics, property.Name, Convert.ToString(value, CultureInfo.InvariantCulture));
            }

            return statistics;
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
                    foreach (TrackSymbolViewModel symbol in symbols)
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
                IEnumerable<SymbolModel> symbolModels = symbols.Cast<TrackSymbolViewModel>().Select(m => new SymbolModel(m.Symbol));
                strategyModel.Symbols.AddRange(symbolModels);
            }

            _parent.CloneStrategy(strategyModel);
        }

        private void DoCreateFolder(IList list)
        {
            if (list == null)
                return;

            IEnumerable<string> symbols = list.Cast<TrackSymbolViewModel>().Select(m => m.Symbol);
            _parent.CreateFolder(symbols);
        }

        private void StopTask()
        {
            if (_cancel != null)
            {
                _cancel.Cancel();
                _cancel = null;
            }
        }

        private void ClearRunData()
        {
            Model.Completed = false;
            Model.Logs = null;
            Model.Result = null;
            Model.Statistics = null;

            var charts = Charts;
            charts.Clear();
            Charts = null;
            Charts = charts;

            Logs = null;

            Orders.Clear();
            Holdings.Clear();
            Trades.Clear();
            TrackSymbols.Clear();

            Statistics.Clear();
            IDictionary<string, decimal?> statistics = Statistics;
            Statistics = null;
            Statistics = statistics;

            _loaded = false;
        }

        private void ParseCharts(Result result)
        {
            SyncObservableCollection<ChartViewModel> workCharts = Charts;
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
