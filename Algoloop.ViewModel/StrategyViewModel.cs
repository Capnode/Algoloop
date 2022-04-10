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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics.Contracts;
using Algoloop.ViewModel.Properties;
using Microsoft.Toolkit.Mvvm.Input;
using QuantConnect;

namespace Algoloop.ViewModel
{
    public class StrategyViewModel : ViewModelBase, ITreeViewModel
    {
        public const string DefaultName = "Strategy";
        internal ITreeViewModel _parent;

        private readonly MarketsModel _markets;
        private readonly SettingModel _settings;

        private readonly string[] _exclude = new[] { "symbols", "market", "resolution", "security", "startdate", "enddate", "cash" };
        private bool _isSelected;
        private bool _isExpanded;
        private string _displayName;

        private SymbolViewModel _selectedSymbol;
        private TrackViewModel _selectedTrack;
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
                () => !IsBusy && !Active);
            StopCommand = new RelayCommand(
                () => DoStopCommand(),
                () => !IsBusy && Active);
            CloneCommand = new RelayCommand(() => DoCloneStrategy(), () => !IsBusy);
            CloneAlgorithmCommand = new RelayCommand(
                () => DoCloneAlgorithm(),
                () => !IsBusy && !string.IsNullOrEmpty(Model.AlgorithmLocation));
            ExportCommand = new RelayCommand(
                () => DoExportStrategy(),
                () => !IsBusy);
            DeleteCommand = new RelayCommand(
                () => DoDeleteStrategy(),
                () => !IsBusy);
            DeleteAllTracksCommand = new RelayCommand(
                () => DoDeleteTracks(null),
                () => !IsBusy);
            DeleteSelectedTracksCommand = new RelayCommand<IList>(
                m => DoDeleteTracks(m),
                m => !IsBusy);
            UseParametersCommand = new RelayCommand<IList>(
                m => DoUseParameters(m),
                m => !IsBusy);
            AddSymbolCommand = new RelayCommand(() => DoAddSymbol(), () => !IsBusy);
            DeleteSymbolsCommand = new RelayCommand<IList>(
                m => DoDeleteSymbols(m),
                m => !IsBusy && SelectedSymbol != null);
            ImportSymbolsCommand = new RelayCommand(
                () => DoImportSymbols(),
                () => !IsBusy);
            ExportSymbolsCommand = new RelayCommand<IList>(
                m => DoExportSymbols(m),
                trm => !IsBusy && SelectedSymbol != null);
            TrackDoubleClickCommand = new RelayCommand<TrackViewModel>(
                m => DoSelectItem(m),
                m => !IsBusy);
            MoveUpSymbolsCommand = new RelayCommand<IList>(
                m => OnMoveUpSymbols(m),
                m => !IsBusy && SelectedSymbol != null);
            MoveDownSymbolsCommand = new RelayCommand<IList>(
                m => OnMoveDownSymbols(m),
                m => !IsBusy && SelectedSymbol != null);
            SortSymbolsCommand = new RelayCommand(
                () => Symbols.Sort(),
                () => !IsBusy);
            MoveStrategyCommand = new RelayCommand<ITreeViewModel>(
                m => OnMoveStrategy(m),
                m => !IsBusy);
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
        public RelayCommand DeleteCommand { get; }
        public RelayCommand DeleteAllTracksCommand { get; }
        public RelayCommand<IList> DeleteSelectedTracksCommand { get; }
        public RelayCommand<IList> UseParametersCommand { get; }
        public RelayCommand AddSymbolCommand { get; }
        public RelayCommand<IList> DeleteSymbolsCommand { get; }
        public RelayCommand ActiveCommand { get; }
        public RelayCommand ImportSymbolsCommand { get; }
        public RelayCommand<IList> ExportSymbolsCommand { get; }
        public RelayCommand<TrackViewModel> TrackDoubleClickCommand { get; }
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
        public SyncObservableCollection<TrackViewModel> Tracks { get; }
            = new SyncObservableCollection<TrackViewModel>();
        public SyncObservableCollection<StrategyViewModel> Strategies { get; }
            = new SyncObservableCollection<StrategyViewModel>();
        public SyncObservableCollection<ListViewModel> Lists { get; }
            = new SyncObservableCollection<ListViewModel>();
        public ObservableCollection<DataGridColumn> TrackColumns { get; }
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

        public TrackViewModel SelectedTrack
        {
            get => _selectedTrack;
            set
            {
                SetProperty(ref _selectedTrack, value);
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
            if (pathValue.Contains(path))
                return;

            pathValue += ";" + path;
            Environment.SetEnvironmentVariable("PATH", pathValue);
        }

        internal void UseParameters(TrackViewModel track)
        {
            if (track == null)
                return;

            Parameters.Clear();
            foreach (ParameterViewModel parameter in track.Parameters)
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

            Model.Tracks.Clear();
            foreach (TrackViewModel track in Tracks)
            {
                Model.Tracks.Add(track.Model);
            }

            Model.Strategies.Clear();
            foreach (StrategyViewModel strategy in Strategies)
            {
                Model.Strategies.Add(strategy.Model);
                strategy.DataToModel();
            }
        }

        internal bool DeleteTrack(TrackViewModel track)
        {
            track.StopTrack();
            Debug.Assert(!track.Active);
            return Tracks.Remove(track);
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
            DeleteCommand.NotifyCanExecuteChanged();
            DeleteAllTracksCommand.NotifyCanExecuteChanged();
            DeleteSelectedTracksCommand.NotifyCanExecuteChanged();
            UseParametersCommand.NotifyCanExecuteChanged();
            AddSymbolCommand.NotifyCanExecuteChanged();
            DeleteSymbolsCommand.NotifyCanExecuteChanged();
            ImportSymbolsCommand.NotifyCanExecuteChanged();
            ExportSymbolsCommand.NotifyCanExecuteChanged();
            TrackDoubleClickCommand.NotifyCanExecuteChanged();
            MoveUpSymbolsCommand.NotifyCanExecuteChanged();
            MoveDownSymbolsCommand.NotifyCanExecuteChanged();
            SortSymbolsCommand.NotifyCanExecuteChanged();
            MoveStrategyCommand.NotifyCanExecuteChanged();
            DropDownOpenedCommand.NotifyCanExecuteChanged();
        }

        private void DoDeleteTracks(IList tracks)
        {
            try
            {
                IsBusy = true;
                List<TrackViewModel> list = tracks == null
                    ? Tracks.ToList()
                    : tracks.Cast<TrackViewModel>().ToList();

                foreach (TrackViewModel track in list)
                {
                    DeleteTrack(track);
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

        private void DoSelectItem(TrackViewModel track)
        {
            if (track == null)
                return;

            // No IsBusy
            track.IsSelected = true;
            _parent.SelectedItem = track;
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
                foreach (TrackViewModel track in selected)
                {
                    UseParameters(track);
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
            DataToModel();

            int count = 0;
            var models = GridOptimizerModels(Model, 0);
            int total = models.Count;
            string message = string.Format(
                CultureInfo.InvariantCulture,
                Resources.RunStrategyWithTracks,
                total);
            Messenger.Send(new NotificationMessage(message), 0);

            var tasks = new List<Task>();
            using (var throttler = new SemaphoreSlim(_settings.MaxBacktests))
            {
                foreach (StrategyModel model in models)
                {
                    await throttler.WaitAsync().ConfigureAwait(true);
                    if (!Active) break;
                    count++;
                    var trackModel = new TrackModel(model.AlgorithmName, model);
                    Log.Trace($"Strategy {trackModel.AlgorithmName} {trackModel.Name} {count}({total})");
                    var track = new TrackViewModel(
                        this, trackModel, _markets, _settings);
                    Tracks.Add(track);
                    Task task = track
                        .StartTrackAsync()
                        .ContinueWith(m =>
                        {
                            ExDataGridColumns.AddPropertyColumns(
                                TrackColumns, track.Statistics, "Statistics");
                            throttler.Release();
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks).ConfigureAwait(true);
            }

            Messenger.Send(new NotificationMessage(Active ? Resources.StrategyCompleted : Resources.StrategyAborted), 0);
            Active = false;
        }

        private void DoStopCommand()
        {
            try
            {
                IsBusy = true;
                Active = false;

                // Stop running tracks
                foreach (TrackViewModel track in Tracks)
                {
                    track.StopTrack();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private List<StrategyModel> GridOptimizerModels(
            StrategyModel rawModel, int index)
        {
            var model = new StrategyModel(rawModel);

            var list = new List<StrategyModel>();
            if (index < model.Parameters.Count)
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

            // Copy Symbols
            Symbols.Clear();
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                symbolModel.Validate();
                SymbolViewModel symbolViewModel = new (this, symbolModel);
                Symbols.Add(symbolViewModel);
            }

            // Copy Parameters
            Parameters.Clear();
            foreach (ParameterModel parameter in Model.Parameters)
            {
                ParameterViewModel parameterViewModel = new (parameter);
                Parameters.Add(parameterViewModel);
            }

            UpdateTracksAndColumns();

            Strategies.Clear();
            foreach (StrategyModel strategyModel in Model.Strategies)
            {
                var strategy = new StrategyViewModel(this, strategyModel, _markets,  _settings);
                Strategies.Add(strategy);
            }
        }

        private void UpdateTracksAndColumns()
        {
            TrackColumns.Clear();
            Tracks.Clear();

            TrackColumns.Add(new DataGridCheckBoxColumn()
            {
                Header = "Active",
                Binding = new Binding("Active") {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                }
            });

            ExDataGridColumns.AddTextColumn(
                TrackColumns, "Name", "Model.Name", false, true);
            foreach (TrackModel TrackModel in Model.Tracks)
            {
                var TrackViewModel = new TrackViewModel(
                    this, TrackModel, _markets, _settings);
                Tracks.Add(TrackViewModel);
                ExDataGridColumns.AddPropertyColumns(
                    TrackColumns, TrackViewModel.Statistics, "Statistics");
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

        private void AlgorithmNameChanged(string algorithmName)
        {
            SetDisplayName();
            RaiseCommands();
            if (string.IsNullOrEmpty(algorithmName)) return;
            if (Model.AlgorithmLanguage.Equals(Language.Python)) return;
            string assemblyPath = MainService.FullExePath(Model.AlgorithmLocation);
            if (string.IsNullOrEmpty(assemblyPath)) return;

            try
            {
                Assembly assembly = Assembly.LoadFile(assemblyPath);
                if (assembly == null) return;

                IEnumerable<Type> type = assembly
                    .GetTypes()
                    .Where(m => m.Name.Equals(algorithmName, StringComparison.OrdinalIgnoreCase));
                if (type == null || !type.Any()) return;

                Parameters.Clear();
                foreach (KeyValuePair<string, string> parameter in ParameterAttribute.GetParametersFromType(type.First()))
                {
                    string parameterName = parameter.Key;
                    string parameterType = parameter.Value;
                    if (_exclude.Contains(parameterName)) continue;

                    ParameterModel parameterModel = Model
                        .Parameters
                        .FirstOrDefault(m => m.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase));
                    if (parameterModel == null)
                    {
                        parameterModel = new ParameterModel() { Name = parameterName };
                    }

                    var parameterViewModel = new ParameterViewModel(parameterModel);
                    Parameters.Add(parameterViewModel);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

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
                List<SymbolViewModel> list = symbols.Cast<SymbolViewModel>()?.ToList();
                Debug.Assert(list != null);

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
                InitialDirectory = Directory.GetCurrentDirectory(),
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
                        foreach (string name in line.Split(',').Where(m => !string.IsNullOrWhiteSpace(m)))
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

                // Load assemblies of algorithms
                string assemblyPath = MainService.FullExePath(Model.AlgorithmLocation);
                Assembly assembly = Assembly.LoadFile(assemblyPath);
                if (string.IsNullOrEmpty(Model.Name))
                {
                    Model.Name = assembly.ManifestModule.Name;
                }

                //Get the list of extention classes in the library: 
                List<string> extended = Loader.GetExtendedTypeNames(assembly);
                List<string> list = assembly.ExportedTypes
                    .Where(m => extended.Contains(m.FullName))
                    .Select(m => m.Name)
                    .ToList();
                list.Sort();

                // Iterate and clone strategies
                foreach (string algorithm in list)
                {
                    if (algorithm.Equals(Model.AlgorithmName, StringComparison.OrdinalIgnoreCase))
                        continue; // Skip this algorithm

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
                InitialDirectory = Directory.GetCurrentDirectory(),
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
                List<SymbolViewModel> list = symbols.Cast<SymbolViewModel>()?.ToList();
                Debug.Assert(list != null);

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
                List<SymbolViewModel> list = symbols.Cast<SymbolViewModel>()?.ToList();
                Debug.Assert(list != null);

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
