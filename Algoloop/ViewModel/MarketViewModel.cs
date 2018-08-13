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
using QuantConnect.Brokerages.Fxcm;
using QuantConnect.Logging;
using QuantConnect.ToolBox.FxcmDownloader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Algoloop.ViewModel
{
    public class MarketViewModel : ViewModelBase
    {
        private MarketsViewModel _parent;
        private Task _task;
        private CancellationTokenSource _cancel;

        public MarketViewModel(MarketsViewModel marketsViewModel, MarketModel marketModel)
        {
            _parent = marketsViewModel;
            Model = marketModel;

            DeleteMarketCommand = new RelayCommand(() => _parent?.DeleteMarket(this), true);
            EnabledCommand = new RelayCommand(() => ProcessMarket(Model.Enabled), true);

            ProcessMarket(Model.Enabled);
        }

        ~MarketViewModel()
        {
            StopTask();
        }

        public MarketModel Model { get; }

        public RelayCommand DeleteMarketCommand { get; }

        public bool Enabled
        {
            get => Model.Enabled;
            set
            {
                Model.Enabled = value;
                RaisePropertyChanged(() => Enabled);
            }
        }

        public RelayCommand EnabledCommand { get; }

        public SyncObservableCollection<PositionViewModel> Positions { get; } = new SyncObservableCollection<PositionViewModel>();
        public SyncObservableCollection<BalanceViewModel> Balances { get; } = new SyncObservableCollection<BalanceViewModel>();

        private void ProcessMarket(bool value)
        {
            Log.Trace($"ConnectMarket {value}");
            if (!value)
            {
                StopTask();
                return;
            }

            _cancel = new CancellationTokenSource();
            _task = Task.Run(() => StartTask(_cancel.Token), _cancel.Token);
        }

        private void StopTask()
        {
            if (_task != null && _task.Status.Equals(TaskStatus.Running))
            {
                _cancel.Cancel();
                _task.Wait();
                Debug.Assert(_task.IsCompleted);
                _task = null;
            }
        }

        private void StartTask(CancellationToken cancel)
        {
            try
            {

                var brokerageFactory = new FxcmBrokerageFactory();
                var brokerageData = brokerageFactory.BrokerageData;
                FxcmDataDownloader downloader = new FxcmDataDownloader(brokerageData["fxcm-server"], brokerageData["fxcm-terminal"], Model.Login, Model.Password);

                IList<string> tickers = new List<string>() { "EURUSD" };
                var startDate = new DateTime(2018, 8, 1);
                var endDate = new DateTime(2018, 8, 10);

                FxcmDownloaderProgram.FxcmDownloader(tickers, "all", startDate, endDate);

                Enabled = false;

            }
            catch (Exception ex)
            {
                Log.Error($"{ex.GetType()}: {ex.Message}");
                Enabled = false;
            }
        }
    }
}
