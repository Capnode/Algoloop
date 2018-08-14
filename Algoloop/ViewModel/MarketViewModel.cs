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
using System.Threading;
using System.Threading.Tasks;

namespace Algoloop.ViewModel
{
    public class MarketViewModel : ViewModelBase
    {
        private MarketsViewModel _parent;
        private CancellationTokenSource _cancel = new CancellationTokenSource();

        public MarketViewModel(MarketsViewModel marketsViewModel, MarketModel marketModel, IAppDomainService appDomainService)
        {
            _parent = marketsViewModel;
            Model = marketModel;
            _appDomainService = appDomainService;

            DeleteMarketCommand = new RelayCommand(() => _parent?.DeleteMarket(this), true);
            EnabledCommand = new RelayCommand(() => ProcessMarket(Model.Enabled), true);

            ProcessMarket(Model.Enabled);
        }

        ~MarketViewModel()
        {
        }

        public MarketModel Model { get; }

        private IAppDomainService _appDomainService;

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

        private async void ProcessMarket(bool enabled)
        {
            if (enabled)
            {
                DataToModel();
                await Task.Run(() => _appDomainService.Run(Model), _cancel.Token);
                DataFromModel();
            }
        }

        internal void DataToModel()
        {
        }

        internal void DataFromModel()
        {
        }
    }
}
