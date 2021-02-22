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
using Algoloop.Support;
using Algoloop.Wpf.Common;
using Algoloop.Wpf.Lean;
using Algoloop.Wpf.Properties;
using Algoloop.Wpf.ViewSupport;
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
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace Algoloop.Wpf.ViewModel
{
    public class TrackViewModel: ViewModel, ITreeViewModel, IComparable, IDisposable
    {
        private bool _isDisposed = false; // To detect redundant calls
        public const string Folder = "Tracks";
        private const string _logFile = "Logs.log";
        private const string _resultFile = "Result.json";
        private const string _zipFile = "track.zip";
        private const double _daysInYear = 365.24;

        private readonly StrategyViewModel _parent;
        private readonly AccountsModel _accounts;
        private readonly SettingModel _settings;
        private static readonly object _mutex = new object();

        private TrackModel _model;
        private bool _isSelected;
        private bool _isExpanded;
        private SyncObservableCollection<ChartViewModel> _charts = new SyncObservableCollection<ChartViewModel>();
        private IDictionary<string, decimal?> _statistics;
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
        private readonly LeanLauncher _leanLauncher = new LeanLauncher();

        public TrackViewModel(StrategyViewModel parent, TrackModel model, AccountsModel accounts, SettingModel settings)
        {
            _parent = parent;
            Model = model;
            _accounts = accounts;
            _settings = settings;

            ActiveCommand = new RelayCommand(() => DoActiveCommand(Model.Active), !IsBusy);
            StartCommand = new RelayCommand(() => DoStartCommand(), () => !IsBusy && !Active);
            StopCommand = new RelayCommand(() => DoStopCommand(), () => !IsBusy && Active);
            DeleteCommand = new RelayCommand(() => DoDeleteTrack(), () => !IsBusy && !Active);
            UseParametersCommand = new RelayCommand(() => DoUseParameters(), () => !IsBusy && !Active);
            ExportSymbolsCommand = new RelayCommand<IList>(m => DoExportSymbols(m), m => !IsBusy);
            CloneStrategyCommand = new RelayCommand<IList>(m => DoCloneStrategy(m), m => !IsBusy);
            ExportCommand = new RelayCommand(() => { }, () => false);
            CloneCommand = new RelayCommand(() => DoCloneStrategy(null), () => !IsBusy);
            CloneAlgorithmCommand = new RelayCommand(() => { }, () => false);

            DataFromModel();
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public bool IsBusy
        {
            get => _parent.IsBusy;
            set => _parent.IsBusy = value;
        }

        public ITreeViewModel SelectedItem
        {
            get => _parent.SelectedItem;
            set => _parent.SelectedItem = value;
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

        public IList SelectedItems
        {
            get { return _selectedItems; }
            set
            {
                Contract.Requires(value != null);
                _selectedItems = value;
                string message = string.Empty;
                if (_selectedItems?.Count > 0)
                {
                    message = string.Format(CultureInfo.InvariantCulture, Resources.SelectedCount, _selectedItems.Count);
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
                ExportSymbolsCommand.RaiseCanExecuteChanged();
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _leanLauncher.Dispose();
                }

                _isDisposed = true;
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

            _parent?.DeleteTrack(this);
        }

        public int CompareTo(object obj)
        {
            var a = obj as TrackViewModel;
            return string.Compare(Model.Name, a?.Model.Name, StringComparison.OrdinalIgnoreCase);
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

        internal async Task StartTrackAsync()
        {
            Debug.Assert(Active);
            ClearRunData();

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
            string folder = Path.GetDirectoryName(MainService.FullExePath(Model.AlgorithmLocation));
            string exeFolder = MainService.GetProgramFolder();
            if (!string.IsNullOrEmpty(folder) && !exeFolder.Equals(folder, StringComparison.OrdinalIgnoreCase))
            {
                StrategyViewModel.AddPath(folder);
            }

            TrackModel model = Model;
            try
            {
                if (Desktop && _settings.DesktopPort > 0)
                {
                    Port = _settings.DesktopPort.ToString(CultureInfo.InvariantCulture);
                    model = await RunTrack(account, model)
                        .ConfigureAwait(false);
                    Port = null;
                }
                else
                {
                    model = await RunTrack(account, model)
                        .ConfigureAwait(false);
                }

                // Split result and logs to separate files
                if (model.Completed)
                {
                    SplitModelToFiles(model);
                }
            }
            catch (Exception ex)
            {
                Log.Trace($"{ex.GetType()}: {ex.Message}");
                throw;
            }

            // Update view
            Model = null;
            Model = model;

            Active = false;
            UiThread(() => DataFromModel());
        }

        internal void StopTrack()
        {
            if (Active)
            {
                Active = false;
                _leanLauncher.Abort();
            }
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

            // Get symbols from model
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                var symbolViewModel = new SymbolViewModel(null, symbolModel);
                Symbols.Add(symbolViewModel);
            }

            // Get parameters from model
            foreach (ParameterModel parameterModel in Model.Parameters)
            {
                var parameterViewModel = new ParameterViewModel(parameterModel);
                Parameters.Add(parameterViewModel);
            }

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

        internal static double CalculateScore(IList<Trade> trades)
        {
            if (trades == null || !trades.Any())
            {
                return 0;
            }

            // Calculate risk
            double worstTrade = (double)trades.Min(m => m.MAE);
//            double maxDrawdown = (double)MaxDrawdown(trades, out _);
            double linearError = -LinearDeviation(trades);
            double risk = Math.Sqrt(worstTrade * linearError);

            // Calculate period
            DateTime first = trades.Min(m => m.EntryTime);
            DateTime last = trades.Max(m => m.ExitTime);
            TimeSpan duration = last - first;
            double years = duration.Ticks / (_daysInYear * TimeSpan.TicksPerDay);

            // Calculate score
            double netProfit = (double)trades.Sum(m => m.ProfitLoss - m.TotalFees);
            if (risk == 0 || years == 0) return netProfit.CompareTo(0);
            double score = netProfit / risk / years;
            return Scale(score);
        }

        internal static double CalculateScore(IList<ChartPoint> series)
        {
            int count = series.Count;
            if (count < 2)
            {
                return 0;
            }

            decimal first = series.First().y;
            decimal last = series.Last().y;
            decimal netProfit = last - first;
            decimal avg = netProfit / (count - 1);
            decimal ideal = first;
            decimal error = 0;
            foreach (ChartPoint trade in series)
            {
                decimal diff = trade.y - ideal;
                error += Math.Abs(diff);
                ideal += avg;
            }

            if (error == 0) return decimal.Compare(netProfit, 0);
            double score = (double)(netProfit * count / error);
            return Scale(score);
        }

        internal static decimal CalcRoMaD(IList<Trade> trades)
        {
            decimal netProfit = trades.Sum(m => m.ProfitLoss - m.TotalFees);
            decimal drawdown = MaxDrawdown(trades, out TimeSpan period);
            decimal roMaD = drawdown == 0 ? 0 : netProfit / -drawdown;
            return roMaD;
        }

        internal static decimal CalcSharpe(IList<Trade> trades)
        {
            IEnumerable<decimal> range = trades.Select(m => m.ProfitLoss - m.TotalFees);
            decimal netProfit = range.Sum();
            decimal stddev = StandardDeviation(range);
            decimal sharpe = stddev == 0 ? 0 : netProfit / stddev;
            return sharpe;
        }

        internal static decimal MaxDrawdown(IList<Trade> trades, out TimeSpan period)
        {
            period = TimeSpan.Zero;
            if (trades == null || !trades.Any())
            {
                return 0;
            }

            decimal drawdown = 0;
            decimal top = 0;
            decimal bottom = 0;
            decimal close = 0;
            DateTime topTime = trades.First().EntryTime;
            foreach (var trade in trades)
            {
                if (close + trade.MFE > top)
                {
                    top = close + trade.MFE;
                    bottom = close + trade.ProfitLoss;
                    topTime = trade.ExitTime;
                }
                else
                {
                    bottom = Math.Min(bottom, close + trade.MAE);
                    TimeSpan span = trade.ExitTime - topTime;
                    if (span > period)
                    {
                        period = span;
                    }
                }

                drawdown = Math.Min(drawdown, bottom - top);
                close += trade.ProfitLoss;
            }

            return drawdown;
        }

        internal static decimal StandardDeviation(IEnumerable<decimal> values)
        {
            int count = values.Count();
            if (count == 0)
            {
                return 0;
            }

            //Compute the Average
            decimal avg = values.Average();

            //Perform the Sum of (value-avg)^2
            decimal sum = values.Sum(d => (d - avg) * (d - avg));
            double variance = (double)sum / count;
            return (decimal)Math.Sqrt(variance);
        }

        private static double LinearDeviation(IList<Trade> trades)
        {
            int count = trades.Count;
            if (count == 0)
            {
                return 0;
            }

            IEnumerable<decimal> range = trades.Select(m => m.ProfitLoss - m.TotalFees);
            decimal netProfit = range.Sum();
            decimal avg = netProfit / count;
            decimal profit = 0;
            decimal ideal = 0;
            decimal sum = 0;
            foreach (decimal trade in range)
            {
                profit += trade;
                ideal += avg;
                decimal epsilon = profit - ideal;
                sum += epsilon * epsilon;
            }

            double variance = (double)sum / count;
            return Math.Sqrt(variance);
        }

        private static void AddCustomStatistics(BacktestResult result, IDictionary<string, decimal?> statistics)
        {
            KeyValuePair<string, Chart> chart = result.Charts.FirstOrDefault(
                m => m.Key.Equals("Strategy Equity", StringComparison.OrdinalIgnoreCase));
            if (chart.Equals(default(KeyValuePair<string, Chart>))) return;

            KeyValuePair<string, Series> equity = chart.Value.Series.FirstOrDefault(
                m => m.Key.Equals("Equity", StringComparison.OrdinalIgnoreCase));
            if (equity.Equals(default(KeyValuePair<string, Series>))) return;

            List<ChartPoint> series = equity.Value.Values;
            double score = CalculateScore(series);
            statistics.Add("Score", (decimal)score.RoundToSignificantDigits(4));
        }

        private static double Scale(double x)
        {
            // Adjust scale that x = 1 returns 0.1
            const int c = 99;
            return x / Math.Sqrt(c + x * x);
        }

        private async Task<TrackModel> RunTrack(AccountModel account, TrackModel model)
        {
            Debug.Assert(!_leanLauncher.IsBusy);
            await Task.Run(() => _leanLauncher.Run(model, account, _settings))
                .ConfigureAwait(false);
            return model;
        }

        private void LoadTrack()
        {
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

                using JsonReader reader = new JsonTextReader(resultStream);
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new OrderJsonConverter());
                result = serializer.Deserialize<BacktestResult>(reader);
                if (result == null)
                {
                    return;
                }
            }

            // Load trades
            LoadTrades(result);

            // Validate if statistics same
            IDictionary<string, decimal?> statistics = ReadStatistics(result);
            if (Model.Statistics == null
                || Model.Statistics.Count != statistics.Count
                || Model.Statistics.Except(statistics).Any())
            {
                Trades.Clear();
                return;
            }

            // Trade details
            foreach (Trade trade in Trades)
            {
                TrackSymbolViewModel trackSymbol = TrackSymbols
                    .FirstOrDefault(m => m.Symbol.Equals(trade.Symbol.Value, StringComparison.OrdinalIgnoreCase));
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

        private static void AddStatisticItem(IDictionary<string, decimal?> statistics, string name, string text)
        {
            if (text.Contains("$") && decimal.TryParse(text.Replace("$", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
            {
                name += "$";
            }
            else if (text.Contains("%") && decimal.TryParse(text.Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                name += "%";
            }
            else if (!decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return;
            }

            // Make unique name
            while (statistics.TryGetValue(name, out _))
            {
                name += "+";
            }

            statistics.Add(name, value);
        }

        private void SplitModelToFiles(TrackModel model)
        {
            Debug.Assert(model.Logs != null);
            Debug.Assert(model.Result != null);

            // Create folder for track files
            Directory.CreateDirectory(Folder);
            string zipFileTemplate = Path.Combine(Folder, _zipFile);

            // Save logs and result to zipfile
            lock (_mutex)
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
            Debug.Assert(result != default);
            Debug.Assert(result.Charts.Any());
            if (result != null)
            {
                // Load trades
                LoadTrades(result);

                // Load statistics
                model.Statistics = ReadStatistics(result);

                // Clear trades
                Trades.Clear();
            }

            // Replace results and logs with file references
            model.Result = string.Empty;
            model.Logs = string.Empty;
        }

        private static IDictionary<string, decimal?> ReadStatistics(BacktestResult result)
        {
            IDictionary<string, decimal?> statistics = new SafeDictionary<string, decimal?>();
            AddCustomStatistics(result, statistics);
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
                AddStatisticItem(statistics, property.Name, Convert.ToString(value, CultureInfo.InvariantCulture));
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

        private void LoadTrades(BacktestResult result)
        {
            Debug.Assert(!Trades.Any());
            result.TotalPerformance.ClosedTrades.ForEach(m => Trades.Add(m));
        }

        private static string UniqueFileName(string path)
        {
            int count = 0;
            string candidate;

            do
            {
                count++;
                candidate = string.Format(
                    CultureInfo.InvariantCulture,
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
            try
            {
                IsBusy = true;
                _parent?.UseParameters(this);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void DoActiveCommand(bool value)
        {
            if (value)
            {
                // No IsBusy
                await StartTrackAsync().ConfigureAwait(false);
            }
            else
            {
                try
                {
                    IsBusy = true;
                    _leanLauncher.Abort();
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private async void DoStartCommand()
        {
            // No IsBusy
            Active = true;
            await StartTrackAsync().ConfigureAwait(false);
        }

        private void DoStopCommand()
        {
            try
            {
                IsBusy = true;
                StopTrack();
            }
            finally
            {
                IsBusy = false;
            }
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
                IsBusy = true;
                string fileName = saveFileDialog.FileName;
                using StreamWriter file = File.CreateText(fileName);
                foreach (TrackSymbolViewModel symbol in symbols)
                {
                    file.WriteLine(symbol.Symbol);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed writing {saveFileDialog.FileName}\n");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoCloneStrategy(IList symbols)
        {
            try
            {
                IsBusy = true;
                var strategyModel = new StrategyModel(Model);
                if (symbols != null)
                {
                    strategyModel.Symbols.Clear();
                    IEnumerable<SymbolModel> symbolModels = symbols.Cast<TrackSymbolViewModel>().Select(m => new SymbolModel(m.Symbol, m.Market, m.Security));
                    foreach (SymbolModel symbol in symbolModels)
                    {
                        strategyModel.Symbols.Add(symbol);
                    }
                }

                _parent.CloneStrategy(strategyModel);
            }
            finally
            {
                IsBusy = false;
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
