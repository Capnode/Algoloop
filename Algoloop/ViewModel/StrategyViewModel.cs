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
using Algoloop.Properties;
using Algoloop.Service;
using Algoloop.ViewSupport;
using Capnode.Wpf.DataGrid;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Algoloop.ViewModel
{
    public class StrategyViewModel : ViewModelBase, ITreeViewModel
    {
        public const string DefaultName = "Strategy";

        private readonly StrategiesViewModel _parent;
        private readonly MarketService _markets;
        private readonly AccountService _accounts;
        private readonly SettingService _settings;

        private readonly string[] _exclude = new[] { "symbols", "resolution", "market", "startdate", "enddate", "cash" };
        private bool _isSelected;
        private bool _isExpanded;
        private string _displayName;
        private ObservableCollection<DataGridColumn> _trackColumns = new ObservableCollection<DataGridColumn>();
        private readonly SyncObservableCollection<FolderModel> _folders = new SyncObservableCollection<FolderModel>();

        private SymbolViewModel _selectedSymbol;
        private TrackViewModel _selectedTrack;
        private FolderModel _selectedFolder;
        private IList _selectedItems;

        public StrategyViewModel(StrategiesViewModel parent, StrategyModel model, MarketService markets, AccountService accounts, SettingService settings)
        {
            _parent = parent;
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _markets = markets;
            _accounts = accounts;
            _settings = settings;

            StartCommand = new RelayCommand(() => DoRunStrategy(), () => !IsBusy);
            StopCommand = new RelayCommand(() => { }, () => false);
            CloneCommand = new RelayCommand(() => DoCloneStrategy(), () => !IsBusy);
            CloneAlgorithmCommand = new RelayCommand(() => DoCloneAlgorithm(), () => !IsBusy && !string.IsNullOrEmpty(Model.AlgorithmName));
            ExportCommand = new RelayCommand(() => DoExportStrategy(), () => !IsBusy);
            DeleteCommand = new RelayCommand(() => _parent?.DoDeleteStrategy(this), () => !IsBusy);
            DeleteAllTracksCommand = new RelayCommand(() => DoDeleteTracks(null), () => !IsBusy);
            DeleteSelectedTracksCommand = new RelayCommand<IList>(m => DoDeleteTracks(m), m => !IsBusy);
            UseParametersCommand = new RelayCommand<IList>(m => DoUseParameters(m), m => !IsBusy);
            AddSymbolCommand = new RelayCommand(() => DoAddSymbol(), () => !IsBusy);
            DeleteSymbolsCommand = new RelayCommand<IList>(m => DoDeleteSymbols(m), m => !IsBusy && SelectedSymbol != null);
            ImportSymbolsCommand = new RelayCommand(() => DoImportSymbols(), () => !IsBusy);
            ExportSymbolsCommand = new RelayCommand<IList>(m => DoExportSymbols(m), trm => !IsBusy &&  SelectedSymbol != null);
            TrackDoubleClickCommand = new RelayCommand<TrackViewModel>(m => DoSelectItem(m), m => !IsBusy);
            MoveUpSymbolsCommand = new RelayCommand<IList>(m => OnMoveUpSymbols(m), m => !IsBusy && SelectedSymbol != null);
            MoveDownSymbolsCommand = new RelayCommand<IList>(m => OnMoveDownSymbols(m), m => !IsBusy && SelectedSymbol != null);
            SortSymbolsCommand = new RelayCommand(() => Symbols.Sort(), () => !IsBusy);

            Model.NameChanged += StrategyNameChanged;
            Model.MarketChanged += MarketChanged;
            Model.AlgorithmNameChanged += AlgorithmNameChanged;
            DataFromModel();

            Model.EndDate = DateTime.Now;
        }

        public bool IsBusy
        {
            get => _parent.IsBusy;
            set => _parent.IsBusy = value;
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

        public StrategyModel Model { get; }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();
        public SyncObservableCollection<ParameterViewModel> Parameters { get; } = new SyncObservableCollection<ParameterViewModel>();
        public SyncObservableCollection<TrackViewModel> Tracks { get; } = new SyncObservableCollection<TrackViewModel>();

        public IList SelectedItems
        {
            get { return _selectedItems; }
            set
            {
                _selectedItems = value;
                string message = string.Empty;
                if (_selectedItems?.Count > 0)
                {
                    message = string.Format(CultureInfo.InvariantCulture, Resources.SelectedCount, _selectedItems.Count);
                }

                Messenger.Default.Send(new NotificationMessage(message));
            }
        }

        public SyncObservableCollection<FolderModel> Folders
        {
            get
            {
                var list = new List<FolderModel> { new FolderModel { Name = string.Empty } };
                MarketModel market = _markets.GetMarket(Model.Market);
                if (market != null)
                {
                    list.AddRange(market.Folders);
                }

                _folders.ReplaceRange(list);
                return _folders;
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set => Set(ref _displayName, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                Set(ref _isSelected, value);
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => Set(ref _isExpanded, value);
        }

        public ObservableCollection<DataGridColumn> TrackColumns
        {
            get => _trackColumns;
            set => Set(ref _trackColumns, value);
        }

        public SymbolViewModel SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                Set(ref _selectedSymbol, value);
                DeleteSymbolsCommand.RaiseCanExecuteChanged();
                ExportSymbolsCommand.RaiseCanExecuteChanged();
                MoveUpSymbolsCommand.RaiseCanExecuteChanged();
                MoveDownSymbolsCommand.RaiseCanExecuteChanged();
            }
        }

        public TrackViewModel SelectedTrack
        {
            get => _selectedTrack;
            set
            {
                Set(ref _selectedTrack, value);
            }
        }

        public FolderModel SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                Set(ref _selectedFolder, value);
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
                track.DataToModel();
            }
        }

        internal bool DeleteTrack(TrackViewModel track)
        {
            return Tracks.Remove(track);
        }

        internal void CloneStrategy(StrategyModel strategyModel)
        {
            var strategy = new StrategyViewModel(_parent, strategyModel, _markets, _accounts, _settings);
            _parent.Strategies.Add(strategy);
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
                    Tracks.Remove(track);
                }

                DataToModel();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoSelectItem(TrackViewModel track)
        {
            if (track == null)
                return;

            try
            {
                IsBusy = true;
                track.IsSelected = true;
                _parent.SelectedItem = track;
                IsExpanded = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DoAddSymbol()
        {
            try
            {
                IsBusy = true;
                if (string.IsNullOrEmpty(SelectedFolder?.Name))
                {
                    var symbol = new SymbolViewModel(this, new SymbolModel());
                    Symbols.Add(symbol);
                }
                else
                {
                    IEnumerable<SymbolViewModel> symbols = _markets
                        .GetActiveSymbols(Model.Market, SelectedFolder)
                        .Where(s => !Symbols.Any(p => p.Model.Name.Equals(s)))
                        .Select(m => new SymbolViewModel(this, m));
                    Symbols.AddRange(symbols);
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

        private async void DoRunStrategy()
        {
            // No IsBusy here
            DataToModel();

            int count = 0;
            var models = GridOptimizerModels(Model, 0);
            int total = models.Count;
            string message = string.Format(CultureInfo.InvariantCulture, Resources.RunStrategyWithTracks, total);
            Messenger.Default.Send(new NotificationMessage(message));

            var tasks = new List<Task>();
            using (var throttler = new SemaphoreSlim(_settings.MaxBacktests))
            {
                foreach (StrategyModel model in models)
                {
                    await throttler.WaitAsync().ConfigureAwait(true);
                    count++;
                    var trackModel = new TrackModel(model.AlgorithmName, model);
                    Log.Trace($"Strategy {trackModel.AlgorithmName} {trackModel.Name} {count}({total})");
                    var track = new TrackViewModel(this, trackModel, _accounts, _settings);
                    Tracks.Add(track);
                    Task task = track
                        .StartTaskAsync()
                        .ContinueWith(m =>
                        {
                            ExDataGridColumns.AddPropertyColumns(TrackColumns, track.Statistics, "Statistics");
                            throttler.Release();
                        },
                        TaskScheduler.FromCurrentSynchronizationContext());
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks).ConfigureAwait(true);
            }

            Messenger.Default.Send(new NotificationMessage(Resources.CompletedStrategy));
        }

        private List<StrategyModel> GridOptimizerModels(StrategyModel rawModel, int index)
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
            Symbols.Clear();
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                var symbolViewModel = new SymbolViewModel(this, symbolModel);
                Symbols.Add(symbolViewModel);
            }

            AlgorithmNameChanged(Model.AlgorithmName);

            UpdateTracksAndColumns();
        }

        private void UpdateTracksAndColumns()
        {
            TrackColumns.Clear();
            Tracks.Clear();

            TrackColumns.Add(new DataGridCheckBoxColumn()
            {
                Header = "Active",
                Binding = new Binding("Active") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
            });

            ExDataGridColumns.AddTextColumn(TrackColumns, "Name", "Model.Name", false, true);
            foreach (TrackModel TrackModel in Model.Tracks)
            {
                var TrackViewModel = new TrackViewModel(this, TrackModel, _accounts, _settings);
                Tracks.Add(TrackViewModel);
                ExDataGridColumns.AddPropertyColumns(TrackColumns, TrackViewModel.Statistics, "Statistics");
            }
        }

        private void StrategyNameChanged()
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

        private void MarketChanged()
        {
            MarketModel market = _markets.GetMarket(Model.Market);
            Model.Provider = market.Provider;
            RaisePropertyChanged(() => Folders);
        }

        private void AlgorithmNameChanged(string algorithmName)
        {
            StrategyNameChanged();

            string assemblyPath = MainService.FullExePath(Model.AlgorithmLocation);
            if (string.IsNullOrEmpty(algorithmName)) return;
            if (string.IsNullOrEmpty(assemblyPath)) return;

            CloneAlgorithmCommand.RaiseCanExecuteChanged();
            Parameters.Clear();
            try
            {
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                if (assembly == null) return;

                IEnumerable<Type> type = assembly
                    .GetTypes()
                    .Where(m => m.Name.Equals(algorithmName, StringComparison.OrdinalIgnoreCase));
                if (type == null || !type.Any()) return;

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
            catch (Exception)
            {
            }

            RaisePropertyChanged(() => Parameters);
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
            OpenFileDialog openFileDialog = new OpenFileDialog
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
                    using StreamReader r = new StreamReader(fileName);
                    while (!r.EndOfStream)
                    {
                        string line = r.ReadLine();
                        foreach (string name in line.Split(',').Where(m => !string.IsNullOrWhiteSpace(m)))
                        {
                            if (Model.Symbols.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) == null)
                            {
                                var symbol = new SymbolModel(name);
                                Model.Symbols.Add(symbol);
                            }
                        }
                    }
                }

                DataFromModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal void CreateFolder(IEnumerable<string> symbols)
        {
            MarketModel market = _markets.GetMarket(Model.Market);
            if (market == null)
                return;

            market.AddFolder(new FolderModel(symbols));
        }

        internal void DoCloneStrategy()
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
                Assembly assembly = Assembly.LoadFrom(assemblyPath);

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
                    var strategy = new StrategyViewModel(_parent, strategyModel, _markets, _accounts, _settings);
                    _parent.Strategies.Add(strategy);
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
            SaveFileDialog saveFileDialog = new SaveFileDialog
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
                JsonSerializer serializer = new JsonSerializer();
                var strategies = new StrategyService();
                strategies.Strategies.Add(Model);
                serializer.Serialize(file, strategies);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
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
                foreach (SymbolViewModel symbol in symbols)
                {
                    file.WriteLine(symbol.Model.Name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
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
    }
}
