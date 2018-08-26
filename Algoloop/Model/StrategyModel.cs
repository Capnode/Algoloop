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

using Algoloop.ViewSupport;
using QuantConnect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using static Algoloop.Model.MarketModel;

namespace Algoloop.Model
{
    [DataContract]
    public class StrategyModel
    {
        public StrategyModel()
        {
        }

        public StrategyModel(StrategyModel model)
        {
            Name = model.Name;
            Provider = model.Provider;
            Account = model.Account;
            BarsBack = model.BarsBack;
            StartDate = model.StartDate;
            EndDate = model.EndDate;
            Resolution = model.Resolution;
            InitialCapital = model.InitialCapital;
            PcntCapitalPerPosition = model.PcntCapitalPerPosition;
            AlgorithmLocation = model.AlgorithmLocation;
            AlgorithmName = model.AlgorithmName;
            Symbols = model.Symbols.Select(m => new SymbolModel(m)).ToList();
            Parameters = model.Parameters.Select(m => new ParameterModel(m)).ToList();
        }

        [Category("Information")]
        [DisplayName("Name")]
        [Description("Name of the strategy.")]
        [DataMember]
        public string Name { get; set; } = "Strategy";

        [Category("Broker")]
        [DisplayName("Data provider")]
        [Description("History data provider")]
        [DataMember]
        public DataProvider Provider { get; set; }

        [Category("Broker")]
        [DisplayName("Account")]
        [Description("Trading account name.")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [TypeConverter(typeof(AccountNameConverter))]
        [DataMember]
        public string Account { get; set; }

        [Category("Time")]
        [DisplayName("Bars back")]
        [DataMember]
        public int BarsBack { get; set; }

        [Category("Time")]
        [DisplayName("From date")]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        [DataMember]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Category("Time")]
        [DisplayName("To date")]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        [DataMember]
        public DateTime EndDate { get; set; } = DateTime.Today;

        [Category("Time")]
        [DisplayName("Resolution")]
        [DataMember]
        public Resolution Resolution { get; set; }

        [Category("Capital")]
        [DisplayName("Initial capial")]
        [DataMember]
        public long InitialCapital { get; set; }

        [Category("Capital")]
        [DisplayName("Percent capital per position")]
        [DataMember]
        public double PcntCapitalPerPosition { get; set; }

        [Category("Algorithm")]
        [DisplayName("File location")]
        [Editor(typeof(FilenameEditor), typeof(FilenameEditor))]
        [RefreshProperties(RefreshProperties.All)]
        [DataMember]
        public string AlgorithmLocation { get; set; }

        [Category("Algorithm")]
        [DisplayName("Algorithm name")]
        [TypeConverter(typeof(AlgorithmNameConverter))]
        [DataMember]
        public string AlgorithmName { get; set; }

        [Browsable(false)]
        [DataMember]
        public List<SymbolModel> Symbols { get; } = new List<SymbolModel>();

        [Browsable(false)]
        [DataMember]
        public List<ParameterModel> Parameters { get; } = new List<ParameterModel>();

        [Browsable(false)]
        [DataMember]
        public List<StrategyJobModel> Jobs { get; } = new List<StrategyJobModel>();
    }
}
