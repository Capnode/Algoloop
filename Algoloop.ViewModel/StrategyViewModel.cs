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
using Capnode.Wpf.DataGrid;
using Microsoft.Win32;
using Newtonsoft.Json;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Logging;
using QuantConnect.Parameters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics.Contracts;
using Algoloop.ViewModel.Properties;
using QuantConnect;
using CommunityToolkit.Mvvm.Input;
using Algoloop.ViewModel.Internal;
using static Algoloop.Model.BacktestModel;

namespace Algoloop.ViewModel
{
    public class StrategyViewModel : ViewModelBase, ITreeViewModel
    {
        public const string DefaultName = "Strategy";
        private const string CsvDelimiter = ";";

        internal ITreeViewModel _parent;
        private readonly MarketsModel _markets;
        private readonly SettingModel _settings;
        private readonly string[] _exclude = new[] { "symbols", "market", "resolution", "security", "startdate", "enddate", "cash" };
        private bool _isSelected;
        private bool _isExpanded;
        private string _displayName;
        private SymbolViewModel _selectedSymbol;
        private BacktestViewModel _selectedBacktest;
        private ListViewModel _selectedList;
        private IList _selectedItems;
        private bool _active;

        public StrategyViewModel(ITreeViewModel parent, StrategyModel model, MarketsModel markets, SettingModel settings)
        {
            _parent = parent;
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _markets = markets;
            _settings = settings;

            StartCommand = new RelayCommand(
                () => DoStartCommand(),
                () => !IsBusy);
            StopCommand = new RelayCommand(
                async () => await DoStopCommandAsync().ConfigureAwait(false),
                () => !IsBusy);
            CloneCommand = new RelayCommand(
                () => DoCloneStrategy(),
                () => !IsBusy);
            CloneAlgorithmCommand = new RelayCommand(
                () => DoCloneAlgorithm(),
                () => !IsBusy);
            ExportCommand = new RelayCommand(
                () => DoExportStrategy(),
                () => !IsBusy);
            ExportSelectedBacktestsCommand = new RelayCommand<IList>(
                m => DoExportSelectedBacktests(m),
                _ => !IsBusy);
            DeleteCommand = new RelayCommand(
                () => DoDeleteStrategy(),
                () => !IsBusy);
            DeleteAllBacktestsCommand = new RelayCommand(
                async () => await DoDeleteBacktestsAsync(null),
                () => !IsBusy);
            DeleteSelectedBacktestsCommand = new RelayCommand<IList>(
                async m => await DoDeleteBacktestsAsync(m),
                _ => !IsBusy);
            UseParametersCommand = new RelayCommand<IList>(
                m => DoUseParameters(m),
                _ => !IsBusy);
            AddSymbolCommand = new RelayCommand(
                () => DoAddSymbol(),
                () => !IsBusy);
            DeleteSymbolsCommand = new RelayCommand<IList>(
                m => DoDeleteSymbols(m),
                _ => !IsBusy && SelectedSymbol != null);
            ImportSymbolsCommand = new RelayCommand(
                () => DoImportSymbols(),
                () => !IsBusy);
            ExportSymbolsCommand = new RelayCommand<IList>(
                m => DoExportSymbols(m),
                _ => !IsBusy && SelectedSymbol != null);
            BacktestDoubleClickCommand = new RelayCommand<BacktestViewModel>(
                m => DoSelectItem(m),
                _ => !IsBusy);
            MoveUpSymbolsCommand = new RelayCommand<IList>(
                m => OnMoveUpSymbols(m),
                _ => !IsBusy && SelectedSymbol != null);
            MoveDownSymbolsCommand = new RelayCommand<IList>(
                m => OnMoveDownSymbols(m),
                _ => !IsBusy && SelectedSymbol != null);
            SortSymbolsCommand = new RelayCommand(
                () => Symbols.Sort(),
                () => !IsBusy);
            MoveStrategyCommand = new RelayCommand<ITreeViewModel>(
                m => OnMoveStrategy(m),
                _ => !IsBusy);
            DropDownOpenedCommand = new RelayCommand(
                () => DoDropDownOpenedCommand(),
                () => !IsBusy);

            Model.NameChanged += SetDisplayName;
            Model.AlgorithmNameChanged += AlgorithmNameChanged;
            DataFromModel();

            Model.EndDate = DateTime.Now;
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        public bool IsBusy
        {
            get => _parent.IsBusy;
            set
            {
                Debug.Assert(_parent != this);
                _parent.IsBusy = value;
                RaiseCommands();
            }
        }

        public ITreeViewModel SelectedItem
        {
            get => _parent.SelectedItem;
            set
            {
                Debug.Assert(_parent != this);
                Debug.Assert(!IsBusy, "Can not set Command execute if busy");
                _parent.SelectedItem = value;
            }
        }

        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand CloneCommand { get; }
        public RelayCommand CloneAlgorithmCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand<IList> ExportSelectedBacktestsCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand DeleteAllBacktestsCommand { get; }
        public RelayCommand<IList> DeleteSelectedBacktestsCommand { get; }
        public RelayCommand<IList> UseParametersCommand { get; }
        public RelayCommand AddSymbolCommand { get; }
        public RelayCommand<IList> DeleteSymbolsCommand { get; }
        public RelayCommand ImportSymbolsCommand { get; }
        public RelayCommand<IList> ExportSymbolsCommand { get; }
        public RelayCommand<BacktestViewModel> BacktestDoubleClickCommand { get; }
        public RelayCommand<IList> MoveUpSymbolsCommand { get; }
        public RelayCommand<IList> MoveDownSymbolsCommand { get; }
        public RelayCommand SortSymbolsCommand { get; }
        public RelayCommand<ITreeViewModel> MoveStrategyCommand { get; }
        public RelayCommand DropDownOpenedCommand { get; }
        public StrategyModel Model { get; }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; }
            = new SyncObservableCollection<SymbolViewModel>();
        public SyncObservableCollection<ParameterViewModel> Parameters { get; }
            = new SyncObservableCollection<ParameterViewModel>();
        public SyncObservableCollection<BacktestViewModel> Backtests { get; }
            = new SyncObservableCollection<BacktestViewModel>();
        public SyncObservableCollection<StrategyViewModel> Strategies { get; }
            = new SyncObservableCollection<StrategyViewModel>();
        public SyncObservableCollection<ListViewModel> Lists { get; }
            = new SyncObservableCollection<ListViewModel>();
        public ObservableCollection<DataGridColumn> BacktestColumns { get; }
            = new ObservableCollection<DataGridColumn>();

        public bool Active 
        {
            get => _active;
            set
            {
                SetProperty(ref _active, value);
                RaiseCommands();
            }
        }
                
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
                    message = string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.SelectedCount,
                        _selectedItems.Count);
                }

                Messenger.Send(new NotificationMessage(message), 0);
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                Debug.Assert(!IsBusy, "Can not set Command execute if busy");
                SetProperty(ref _isSelected, value);
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public SymbolViewModel SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                SetProperty(ref _selectedSymbol, value);
                RaiseCommands();
            }
        }

        public BacktestViewModel SelectedBacktest
        {
            get => _selectedBacktest;
            set
            {
                SetProperty(ref _selectedBacktest, value);
                RaiseCommands();
            }
        }

        public ListViewModel SelectedList
        {
            get => _selectedList;
            set
            {
                SetProperty(ref _selectedList, value);
                RaiseCommands();
            }
        }

        public void Refresh()
        {
            Model.Refresh();
        }

        internal static void AddPath(string path)
        {
            string pathValue = Environment.GetEnvironmentVariable("PATH");
            if (pathValue!.Contains(path))
                return;

            pathValue += ";" + path;
            Environment.SetEnvironmentVariable("PATH", pathValue);
        }

        internal void UseParameters(BacktestViewModel backtest)
        {
            if (backtest == null)
                return;

            Parameters.Clear();
            foreach (ParameterViewModel parameter in backtest.Parameters)
            {
                Parameters.Add(parameter);
            }
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

            Model.Backtests.Clear();
            foreach (BacktestViewModel backtest in Backtests)
            {
                Model.Backtests.Add(backtest.Model);
            }

            Model.Strategies.Clear();
            foreach (StrategyViewModel strategy in Strategies)
            {
                Model.Strategies.Add(strategy.Model);
                strategy.DataToModel();
            }
        }

        internal async Task<bool> DeleteBacktestAsync(BacktestViewModel backtest)
        {
            await backtest.StopBacktestAsync();
            Debug.Assert(!backtest.Active);
            return Backtests.Remove(backtest);
        }

        internal void CloneStrategy(StrategyModel strategyModel)
        {
            var strategy = new StrategyViewModel(this, strategyModel, _markets, _settings);
            if (_parent is StrategyViewModel strategyVm)
            {
                strategyVm.Strategies.Add(strategy);
            }
            else if (_parent is StrategiesViewModel strategiesVm)
            {
                strategiesVm.Strategies.Add(strategy);
            }
        }

        private void RaiseCommands()
        {
            StartCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
            CloneCommand.NotifyCanExecuteChanged();
            CloneAlgorithmCommand.NotifyCanExecuteChanged();
            ExportCommand.NotifyCanExecuteChanged();
            ExportSelectedBacktestsCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
            DeleteAllBacktestsCommand.NotifyCanExecuteChanged();
            DeleteSelectedBacktestsCommand.NotifyCanExecuteChanged();
            UseParametersCommand.NotifyCanExecuteChanged();
            AddSymbolCommand.NotifyCanExecuteChanged();
            DeleteSymbolsCommand.NotifyCanExecuteChanged();
            ImportSymbolsCommand.NotifyCanExecuteChanged();
            ExportSymbolsCommand.NotifyCanExecuteChanged();
            BacktestDoubleClickCommand.NotifyCanExecuteChanged();
            MoveUpSymbolsCommand.NotifyCanExecuteChanged();
            MoveDownSymbolsCommand.NotifyCanExecuteChanged();
            SortSymbolsCommand.NotifyCanExecuteChanged();
            MoveStrategyCommand.NotifyCanExecuteChanged();
            DropDownOpenedCommand.NotifyCanExecuteChanged();
        }

        private async Task DoDeleteBacktestsAsync(IList backtests)
        {
            try
            {
                IsBusy = true;
                List<BacktestViewModel> list = backtests == null
                    ? Backtests.ToList()
                    : backtests.Cast<BacktestViewModel>().ToList();

                foreach (BacktestViewModel backtest in list)
                {
                    await DeleteBacktestAsync(backtest);
                }

                DataToModel();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoDeleteStrategy()
        {
            // No IsBusy
            DeleteStrategy();
        }

        private void DeleteStrategy()
        {
            if (_parent is StrategiesViewModel strategies)
            {
                strategies.DeleteStrategy(this);
            }
            else if (_parent is StrategyViewModel strategy)
            {
                strategy.DeleteStrategy(this);
            }
        }

        private void DeleteStrategy(StrategyViewModel strategy)
        {
            Debug.Assert(!strategy.Active);
            Strategies.Remove(strategy);
        }

        private void DoSelectItem(BacktestViewModel backtest)
        {
            if (backtest == null)
                return;

            // No IsBusy
            backtest.IsSelected = true;
            _parent.SelectedItem = backtest;
            IsExpanded = true;
        }

        private void DoAddSymbol()
        {
            try
            {
                IsBusy = true;
                if (SelectedList?.Model == null)
                {
                    var symbol = new SymbolViewModel(
                        this,
                        new SymbolModel("symbol", Model.Market, Model.Security));
                    Symbols.Add(symbol);
                }
                else
                {
                    Symbols.AddRange(SelectedList.Symbols);
                }

                DataToModel();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoUseParameters(IList selected)
        {
            if (selected == null)
                return;

            try
            {
                IsBusy = true;
                foreach (BacktestViewModel backtest in selected)
                {
                    UseParameters(backtest);
                    break; // skip rest
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void DoStartCommand()
        {
            // No IsBusy here
            Active = true;
            int count = 0;
            List<(StrategyViewModel, StrategyModel)> models = CreateRunList(this);
            int total = models.Count;
            string message = string.Format(
                CultureInfo.InvariantCulture,
                Resources.StrategyStarted,
                total);
            Messenger.Send(new NotificationMessage(message), 0);

            var tasks = new List<Task>();
            CompletionStatus status = CompletionStatus.Success;
            foreach ((StrategyViewModel vm, StrategyModel model) in models)
            {
                await BacktestManager.Wait().ConfigureAwait(true);
                if (!Active) break;
                count++;
                var backtestModel = new BacktestModel(model.AlgorithmName, model);
                Log.Trace($"Strategy {backtestModel.AlgorithmName} {backtestModel.Name} {count}({total})");
                var backtest = new BacktestViewModel(vm, backtestModel, _markets, _settings);
                vm.Backtests.Add(backtest);
                Task task = backtest
                    .StartBacktestAsync()
                    .ContinueWith(_ =>
                    {
                        if (backtestModel.Status > status)
                        {
                            status = backtestModel.Status;
                        }

                        BacktestManager.Release();
                    });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(true);
            message = BacktestViewModel.ToMessage(status);
            Messenger.Send(new NotificationMessage(message), 0);
            Active = false;
        }

        private static List<(StrategyViewModel, StrategyModel)> CreateRunList(StrategyViewModel strategyViewModel)
        {
            List<(StrategyViewModel, StrategyModel)> list = new();
            foreach (StrategyViewModel strategy in strategyViewModel.Strategies)
            {
                 list.AddRange(CreateRunList(strategy));
            }

            strategyViewModel.DataToModel();
            List<StrategyModel> models = GridOptimizerModels(strategyViewModel.Model, 0);
            list.AddRange(models.Select(m => (strategyViewModel, m)));
            return list;
        }

        private async Task DoStopCommandAsync()
        {
            try
            {
                IsBusy = true;
                Active = false;

                Messenger.Send(new NotificationMessage(Resources.StrategyAborting), 0);

                // Stop running backtests
                string message = Resources.StrategyAborted;
                foreach (BacktestViewModel backtest in Backtests)
                {
                    if (!await backtest.StopBacktestAsync(false))
                    {
                        message = Resources.StrategyAbortFailed;
                    }
                }

                Messenger.Send(new NotificationMessage(message), 0);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static List<StrategyModel> GridOptimizerModels(
            StrategyModel rawModel, int index)
        {
            var model = new StrategyModel(rawModel);

            var list = new List<StrategyModel>();
            if (string.IsNullOrEmpty(model.AlgorithmName))
            {
                // Ignore
            }
            else if (index < model.Parameters.Count)
            {
                var parameter = model.Parameters[index];
                if (parameter.UseRange)
                {
                    foreach (string value in SplitRange(parameter.Range))
                    {
                        parameter.Value = value;
                        parameter.UseValue = true;
                        list.AddRange(GridOptimizerModels(model, index + 1));
                    }
                }
                else
                {
                    list.AddRange(GridOptimizerModels(model, index + 1));
                }
            }
            else
            {
                list.Add(model);
            }

            return list;
        }

        private static IEnumerable<string> SplitRange(string range)
        {
            foreach (string value in range.Split(','))
            {
                yield return value;
            }
        }

        private void DataFromModel()
        {
            Debug.Assert(IsUiThread(), "Not UI thread!");
            SetDisplayName();
            SetParameters();

            // Copy Symbols
            Symbols.Clear();
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                symbolModel.Validate();
                SymbolViewModel symbolViewModel = new (this, symbolModel);
                Symbols.Add(symbolViewModel);
            }

            // Copy Strategies
            Strategies.Clear();
            foreach (StrategyModel strategyModel in Model.Strategies)
            {
                var strategy = new StrategyViewModel(this, strategyModel, _markets,  _settings);
                Strategies.Add(strategy);
            }

            UpdateBacktestsAndColumns();
        }

        private void UpdateBacktestsAndColumns()
        {
            BacktestColumns.Clear();
            Backtests.Clear();

            BacktestColumns.Add(new DataGridCheckBoxColumn()
            {
                Header = "Active",
                Binding = new Binding("Active") {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                }
            });

            ExDataGridColumns.AddTextColumn(
                BacktestColumns, "Name", "Model.Name", false, true);
            foreach (BacktestModel BacktestModel in Model.Backtests)
            {
                var backtestViewModel = new BacktestViewModel(
                    this, BacktestModel, _markets, _settings);
                Backtests.Add(backtestViewModel);
                ExDataGridColumns.AddPropertyColumns(
                    BacktestColumns, backtestViewModel.Statistics, "Statistics");
            }
        }

        private void SetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(Model.Name))
            {
                DisplayName = Model.Name;
            }
            else if (!string.IsNullOrWhiteSpace(Model.AlgorithmName))
            {
                DisplayName = Model.AlgorithmName;
            }
            else
            {
                DisplayName = DefaultName;
            }
        }

        private void SetParameters()
        {
            if (string.IsNullOrEmpty(Model.AlgorithmName)) return;
            if (Model.AlgorithmLanguage.Equals(Language.Python)) return;
            string assemblyPath = MainService.FullExePath(Model.AlgorithmLocation);
            if (string.IsNullOrEmpty(assemblyPath)) return;

            try
            {
                Assembly assembly = Assembly.LoadFile(assemblyPath);
                IEnumerable<Type> type = assembly
                    .GetTypes()
                    .Where(m => m.Name.Equals(Model.AlgorithmName, StringComparison.OrdinalIgnoreCase));
                if (!type.Any()) return;

                Parameters.Clear();
                foreach (KeyValuePair<string, string> parameter in ParameterAttribute.GetParametersFromType(type.First()))
                {
                    string parameterName = parameter.Key;
                    string parameterType = parameter.Value;
                    if (_exclude.Contains(parameterName)) continue;

                    ParameterModel parameterModel = Model
                        .Parameters
                        .FirstOrDefault(m => m.Name.Equals(parameterName, StringComparison.Ordinal));
                    parameterModel ??= new ParameterModel() { Name = parameterName };

                    var parameterViewModel = new ParameterViewModel(parameterModel);
                    Parameters.Add(parameterViewModel);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void AlgorithmNameChanged()
        {
            SetDisplayName();
            SetParameters();
            RaiseCommands();
            OnPropertyChanged(nameof(Parameters));
        }

        private void DoDeleteSymbols(IList symbols)
        {
            Debug.Assert(symbols != null);
            if (Symbols.Count == 0 || symbols.Count == 0)
                return;

            try
            {
                IsBusy = true;
                // Create a copy of the list before remove
                List<SymbolViewModel> list = symbols.Cast<SymbolViewModel>().ToList();
                int pos = Symbols.IndexOf(list.First());
                foreach (SymbolViewModel symbol in list)
                {
                    Symbols.Remove(symbol);
                }

                DataToModel();
                if (Symbols.Count > 0)
                {
                    SelectedSymbol = Symbols[Math.Min(pos, Symbols.Count - 1)];
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoImportSymbols()
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = MainService.GetUserDataFolder(),
                Multiselect = false,
                Filter = "symbol file (*.csv)|*.csv|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == false)
                return;

            try
            {
                IsBusy = true;
                foreach (string fileName in openFileDialog.FileNames)
                {
                    using var r = new StreamReader(fileName);
                    while (!r.EndOfStream)
                    {
                        string line = r.ReadLine();
                        foreach (string name in line!.Split(',').Where(m => !string.IsNullOrWhiteSpace(m)))
                        {
                            if (Model.Symbols.FirstOrDefault(m => m.Id.Equals(name, StringComparison.OrdinalIgnoreCase)) == null)
                            {
                                var symbol = new SymbolModel(name, Model.Market, Model.Security);
                                Model.Symbols.Add(symbol);
                            }
                        }
                    }
                }

                DataFromModel();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed reading {openFileDialog.FileName}\n");
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal void AddStrategy(StrategyViewModel strategy)
        {
            Debug.Assert(strategy != this);
            strategy._parent = this;
            Strategies.Add(strategy);
        }

        private void DoCloneStrategy()
        {
            try
            {
                IsBusy = true;
                DataToModel();
                var strategyModel = new StrategyModel(Model);
                CloneStrategy(strategyModel);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoCloneAlgorithm()
        {
            try
            {
                IsBusy = true;
                DataToModel();
                List<string> list = null;
                if (Model.AlgorithmLanguage == Language.Python)
                {
                    string[] pyFiles = Directory.GetFiles(Model.AlgorithmFolder, "*.py");
                    list = pyFiles.Select(Path.GetFileNameWithoutExtension).ToList();
                }
                else
                {
                    // Load assemblies of algorithms
                    string assemblyPath = MainService.FullExePath(Model.AlgorithmLocation);
                    Assembly assembly = Assembly.LoadFile(assemblyPath);
                    if (string.IsNullOrEmpty(Model.Name))
                    {
                        Model.Name = assembly.ManifestModule.Name;
                    }

                    //Get the list of extention classes in the library: 
                    List<string> extended = Loader.GetExtendedTypeNames(assembly);
                    list = assembly.ExportedTypes
                        .Where(m => extended.Contains(m.FullName))
                        .Select(m => m.Name)
                        .ToList();
                }

                // Iterate and clone strategies
                list.Sort();
                foreach (string algorithm in list)
                {
                    if (algorithm.Equals(Model.AlgorithmName, StringComparison.OrdinalIgnoreCase)) continue; // Skip this algorithm
                    var strategyModel = new StrategyModel(Model)
                    {
                        AlgorithmName = algorithm,
                        Name = null
                    };
                    var strategy = new StrategyViewModel(this, strategyModel, _markets, _settings);
                    Strategies.Add(strategy);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoExportStrategy()
        {
            DataToModel();
            var saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = MainService.GetUserDataFolder(),
                FileName = Model.Name,
                Filter = "json file (*.json)|*.json|All files (*.*)|*.*"
            };
            if (saveFileDialog.ShowDialog() == false)
                return;

            try
            {
                IsBusy = true;
                string fileName = saveFileDialog.FileName;
                using StreamWriter file = File.CreateText(fileName);
                var serializer = new JsonSerializer();
                var strategies = new StrategiesModel();
                strategies.Strategies.Add(Model);
                serializer.Serialize(file, strategies);
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

        private void DoExportSelectedBacktests(IList backtests)
        {
            DataToModel();
            var saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = MainService.GetUserDataFolder(),
                FileName = DisplayName,
                Filter = "CSV file (*.csv)|*.csv|All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == false)
                return;

            try
            {
                IsBusy = true;
                IEnumerable<string> headers = BacktestColumns.Select(m => m.Header.ToString());
                File.WriteAllText(saveFileDialog.FileName, string.Join(CsvDelimiter, headers) + Environment.NewLine);
                List<BacktestViewModel> list = backtests.Count == 0 ? Backtests.ToList() : backtests.Cast<BacktestViewModel>().ToList();
                foreach (BacktestViewModel backtest in list)
                {
                    string line = backtest.Model.Active + CsvDelimiter + backtest.Model.Name;
                    foreach (string header in headers.Skip(2))
                    {
                        line += CsvDelimiter;
                        if (backtest.Model.Statistics.TryGetValue(header, out decimal? value) && value != null)
                        {
                            line += decimal.Round(value ?? 0, 4).ToString();
                        }
                    }

                    File.AppendAllText(saveFileDialog.FileName, line + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {

                Log.Error(ex, $"Failed writing {saveFileDialog.FileName}\n");
                Messenger.Send(new NotificationMessage(ex.Message), 0);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoExportSymbols(IList symbols)
        {
            Debug.Assert(symbols != null);
            if (Symbols.Count == 0 || symbols.Count == 0)
                return;

            DataToModel();
            var saveFileDialog = new SaveFileDialog
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
                foreach (SymbolViewModel symbol in symbols)
                {
                    file.WriteLine(symbol.Model.Id);
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

        private void OnMoveUpSymbols(IList symbols)
        {
            if (Symbols.Count <= 1)
                return;

            try
            {
                IsBusy = true;

                // Create a copy of the list before move
                List<SymbolViewModel> list = symbols.Cast<SymbolViewModel>().ToList();
                for (int i = 1; i < Symbols.Count; i++)
                {
                    if (list.Contains(Symbols[i]) && !list.Contains(Symbols[i - 1]))
                    {
                        Symbols.Move(i, i - 1);
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnMoveDownSymbols(IList symbols)
        {
            if (Symbols.Count <= 1)
                return;

            try
            {
                IsBusy = true;

                // Create a copy of the list before move
                List<SymbolViewModel> list = symbols.Cast<SymbolViewModel>().ToList();
                for (int i = Symbols.Count - 2; i >= 0; i--)
                {
                    if (list.Contains(Symbols[i]) && !list.Contains(Symbols[i + 1]))
                    {
                        Symbols.Move(i, i + 1);
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnMoveStrategy(ITreeViewModel item)
        {
            // No IsBusy
            if (item is StrategiesViewModel strategies)
            {
                DeleteStrategy();
                strategies.AddStrategy(this);
            }
            else if (item is StrategyViewModel strategy)
            {
                DeleteStrategy();
                strategy.AddStrategy(this);
            }
        }

        private void DoDropDownOpenedCommand()
        {
            var marketsVm = new MarketsViewModel(_markets, _settings);
            Lists.Clear();
            var listVm = new ListViewModel(new MarketViewModel(marketsVm, new ProviderModel(), _settings), null);
            Lists.Add(listVm);
            foreach (ProviderModel market in _markets.Markets)
            {
                var marketVm = new MarketViewModel(marketsVm, market, _settings);
                foreach (ListModel list in market.Lists)
                {
                    listVm = new ListViewModel(marketVm, list);
                    Lists.Add(listVm);
                }
            }
        }
    }
}
