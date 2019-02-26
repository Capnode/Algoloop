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
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.ToolBox;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Algoloop.ViewModel
{
    public class SymbolViewModel : ViewModelBase, ITreeViewModel
    {
        private ITreeViewModel _parent;
        private Resolution _selectedResolution = Resolution.Daily;

        public SymbolViewModel(ITreeViewModel parent, SymbolModel model)
        {
            _parent = parent;
            Model = model;

            DeleteCommand = new RelayCommand(() => { }, () => false);
            StartCommand = new RelayCommand(() => { }, () => false);
            StopCommand = new RelayCommand(() => { }, () => false);
            UpdateCommand = new RelayCommand(() => LoadChart(_parent as MarketViewModel), () => _parent is MarketViewModel);
        }

        public RelayCommand DeleteCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand UpdateCommand { get; }

        public SymbolModel Model { get; }
        public IEnumerable<Resolution> ResolutionList { get; } = new[] { Resolution.Daily, Resolution.Hour, Resolution.Minute, Resolution.Second, Resolution.Tick };
        private SyncObservableCollection<ChartViewModel> _charts = new SyncObservableCollection<ChartViewModel>();
        private DateTime _date = DateTime.Today;

        public bool Active
        {
            get => Model.Active;
            set
            {
                Model.Active = value;
                RaisePropertyChanged(() => Active);
                (_parent as StrategyViewModel)?.Refresh();
                (_parent as MarketViewModel)?.Refresh();
            }
        }

        public Resolution SelectedResolution
        {
            get => _selectedResolution;
            set => Set(ref _selectedResolution, value);
        }

        public DateTime Date
        {
            get => _date;
            set => Set(ref _date, value);
        }

        public SyncObservableCollection<ChartViewModel> Charts
        {
            get => _charts;
            set => Set(ref _charts, value);
        }

        public void Refresh()
        {
            Model.Refresh();

            if (_parent is MarketViewModel market)
            {
                LoadChart(market);
            }
        }

        private void LoadChart(MarketViewModel market)
        {
            Debug.Assert(market != null);
            Charts.Clear();
            string filename = LeanFilepath(market, Model.Name, SelectedResolution, Date);
            if (filename != null)
            {
                var leanDataReader = new LeanDataReader(filename);
                IEnumerable<BaseData> data = leanDataReader.Parse();
                if (data.Any())
                {
                    var series = new Series(Model.Name, SeriesType.Candle, "$", Color.Black);
                    var viewModel = new ChartViewModel(series, data);
                    Charts.Add(viewModel);
                }
            }
        }

        private string LeanFilepath(MarketViewModel market, string symbol, Resolution resolution, DateTime date)
        {
            DirectoryInfo basedir = new DirectoryInfo(market.DataFolder);
            DirectoryInfo[] subdirs = basedir.GetDirectories("*");
            if (resolution.Equals(Resolution.Daily) || resolution.Equals(Resolution.Hour))
            {
                foreach (DirectoryInfo folder in subdirs)
                {
                    var path = Path.Combine(folder.FullName, market.Model.Provider, resolution.ToString());
                    if (!Directory.Exists(path))
                        continue;

                    var dir = new DirectoryInfo(path);
                    var files = dir.GetFiles(symbol + "*.zip").Select(x => x.FullName);
                    if (files.Count() == 1)
                    {
                        return files.Single();
                    }
                }
            }
            else
            {
                foreach (DirectoryInfo folder in subdirs)
                {
                    var path = Path.Combine(folder.FullName, market.Model.Provider, resolution.ToString(), symbol);
                    if (!Directory.Exists(path))
                        continue;

                    var dir = new DirectoryInfo(path);
                    string date1 = date.ToString("yyyyMMdd");
                    var files = dir.GetFiles(date1 + "*.zip").Select(x => x.FullName);
                    if (files.Count() == 1)
                    {
                        return files.Single();
                    }
                }
            }

            return null;
        }
    }
}
