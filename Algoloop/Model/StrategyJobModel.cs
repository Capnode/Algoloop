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
using System.Runtime.Serialization;
using static Algoloop.Model.MarketModel;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class StrategyJobModel
    {
        [Category("Information")]
        [DisplayName("Name")]
        [Description("Name of the job.")]
        [ReadOnly(true)]
        [DataMember]
        public string Name { get; set; } = "Job";

        [Category("Broker")]
        [DisplayName("Data provider")]
        [Description("History data provider")]
        [DataMember]
        public DataProvider Provider { get; set; }

        [Category("Broker")]
        [DisplayName("Account")]
        [Description("Trading account name.")]
        [DataMember]
        public string Account { get; set; }

        [Category("Time")]
        [DisplayName("Bars back")]
        [ReadOnly(true)]
        [DataMember]
        public int BarsBack { get; set; }

        [Category("Time")]
        [DisplayName("From date")]
        [ReadOnly(true)]
        [DataMember]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Category("Time")]
        [DisplayName("To date")]
        [ReadOnly(true)]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        [DataMember]
        public DateTime EndDate { get; set; } = DateTime.Today;

        [Category("Time")]
        [DisplayName("Resolution")]
        [ReadOnly(true)]
        [DataMember]
        public Resolution Resolution { get; set; }

        [Category("Capital")]
        [DisplayName("Initial capial")]
        [ReadOnly(true)]
        [DataMember]
        public long InitialCapital { get; set; }

        [Category("Capital")]
        [DisplayName("Percent capital per position")]
        [ReadOnly(true)]
        [DataMember]
        public double PcntCapitalPerPosition { get; set; }

        [Category("Algorithm")]
        [DisplayName("File location")]
        [ReadOnly(true)]
        [Editor(typeof(FilenameEditor), typeof(FilenameEditor))]
        [DataMember]
        public string AlgorithmLocation { get; set; }

        [Category("Algorithm")]
        [DisplayName("Algorithm name")]
        [ReadOnly(true)]
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
        public string DataFolder { get; set; }

        [Browsable(false)]
        [DataMember]
        public bool Completed { get; set; }

        [Browsable(false)]
        [DataMember]
        public string Result { get; set; }

        [Browsable(false)]
        [DataMember]
        public string Logs { get; set; }

        public StrategyJobModel()
        {
        }

        public StrategyJobModel(string name, StrategyModel strategy)
        {
            Name = name;
            Account = strategy.Account;
            Provider = strategy.Provider;
            BarsBack = strategy.BarsBack;
            StartDate = strategy.StartDate;
            EndDate = strategy.EndDate;
            InitialCapital = strategy.InitialCapital;
            PcntCapitalPerPosition = strategy.PcntCapitalPerPosition;
            AlgorithmLocation = strategy.AlgorithmLocation;
            AlgorithmName = strategy.AlgorithmName;
            Resolution = strategy.Resolution;
            Symbols.AddRange(strategy.Symbols);
            Parameters.AddRange(strategy.Parameters);
        }
    }
}
