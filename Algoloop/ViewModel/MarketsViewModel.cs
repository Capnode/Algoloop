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
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace Algoloop.ViewModel
{
    public class MarketsViewModel
    {
        public MarketsModel Model { get; private set; }

        private readonly IAppDomainService _appDomainService;

        public SyncObservableCollection<MarketViewModel> Markets { get; } = new SyncObservableCollection<MarketViewModel>();

        public RelayCommand AddMarketCommand { get; }

        public MarketsViewModel(MarketsModel model, IAppDomainService appDomainService)
        {
            Model = model;
            _appDomainService = appDomainService;

            AddMarketCommand = new RelayCommand(() => AddMarket(), true);
            Messenger.Default.Register<NotificationMessageAction<List<MarketModel>>>(this, (message) => OnNotificationMessage(message));
            DataFromModel();
        }

        private void OnNotificationMessage(NotificationMessageAction<List<MarketModel>> message)
        {
            if (string.IsNullOrEmpty(message.Notification))
            {
                message.Execute(Markets.Select(m => m.Model).ToList());
            }
            else
            {
                message.Execute(Markets.Where(s => s.Model.Name.Equals(message.Notification)).Select(m => m.Model).ToList());
            }
        }

        internal bool DeleteMarket(MarketViewModel loginViewModel)
        {
            bool ok = Markets.Remove(loginViewModel);
            Debug.Assert(ok);
            return ok;
        }

        private void AddMarket()
        {
            var loginViewModel = new MarketViewModel(this, new MarketModel(), _appDomainService);
            Markets.Add(loginViewModel);
        }

        internal bool Read(string fileName)
        {
            if (!File.Exists(fileName))
                return false;

            try
            {
                using (StreamReader r = new StreamReader(fileName))
                {
                    string json = r.ReadToEnd();
                    Model = JsonConvert.DeserializeObject<MarketsModel>(json);
                }

                DataFromModel();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
                return false;
            }
        }

        internal bool Save(string fileName)
        {
            try
            {
                DataToModel();

                using (StreamWriter file = File.CreateText(fileName))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, Model);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString());
                return false;
            }
        }

        private void DataToModel()
        {
            Model.Markets.Clear();
            foreach (MarketViewModel market in Markets)
            {
                Model.Markets.Add(market.Model);
                market.DataToModel();
            }
        }

        private void DataFromModel()
        {
            Markets.Clear();
            foreach (MarketModel market in Model.Markets)
            {
                var viewModel = new MarketViewModel(this, market, _appDomainService);
                Markets.Add(viewModel);
            }
        }
    }
}
