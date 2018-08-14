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

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Algoloop.Model;
using Algoloop.Service;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;

namespace Algoloop.ViewModel
{
    public class StrategiesViewModel
    {
        private readonly IAppDomainService _appDomainService;

        public StrategiesModel Model { get; private set; }

        public SyncObservableCollection<StrategyViewModel> Strategies { get; } = new SyncObservableCollection<StrategyViewModel>();

        public RelayCommand AddStrategyCommand { get; }

        public StrategiesViewModel(StrategiesModel model, IAppDomainService appDomainService)
        {
            Model = model;
            _appDomainService = appDomainService;

            AddStrategyCommand = new RelayCommand(() => AddStrategy(), true);

            DataFromModel();
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
                    Model = JsonConvert.DeserializeObject<StrategiesModel>(json);
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
            Model.Strategies.Clear();
            foreach (StrategyViewModel strategy in Strategies)
            {
                Model.Strategies.Add(strategy.Model);
                strategy.DataToModel();
            }
        }

        private void DataFromModel()
        {
            Strategies.Clear();
            foreach (StrategyModel strategyModel in Model.Strategies)
            {
                var strategyViewModel = new StrategyViewModel(this, strategyModel, _appDomainService);
                Strategies.Add(strategyViewModel);
            }
        }

        internal bool DeleteStrategy(StrategyViewModel strategy)
        {
            bool ok = Strategies.Remove(strategy);
            Debug.Assert(ok);
            return ok;
        }

        private void AddStrategy()
        {
            var strategy = new StrategyViewModel(this, new StrategyModel(), _appDomainService);
            Strategies.Add(strategy);
        }
    }
}
