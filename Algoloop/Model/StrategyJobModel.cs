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
    [Serializable]
    [DataContract]
    public class StrategyJobModel : ModelBase
    {
        [Category("Information")]
        [DisplayName("Name")]
        [Description("Name of the job.")]
        [ReadOnly(false)]
        [Browsable(true)]
        [DataMember]
        public string Name { get; set; } = "Job";

        [Category("Broker")]
        [DisplayName("Data provider")]
        [Description("Market data provider")]
        [ReadOnly(false)]
        [Browsable(true)]
        [DataMember]
        public MarketType Provider { get; set; }

        [Category("Broker")]
        [DisplayName("Account")]
        [Description("Trading account for live or paper trading.")]
        [ReadOnly(false)]
        [Browsable(true)]
        [DataMember]
        public string Account { get; set; }

        [Category("Time")]
        [DisplayName("Bars back")]
        [ReadOnly(true)]
        [Browsable(true)]
        [DataMember]
        public int BarsBack { get; set; }

        [Category("Time")]
        [DisplayName("From date")]
        [ReadOnly(true)]
        [Browsable(true)]
        [DataMember]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Category("Time")]
        [DisplayName("To date")]
        [ReadOnly(true)]
        [Browsable(true)]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        [DataMember]
        public DateTime EndDate { get; set; } = DateTime.Today;

        [Category("Time")]
        [DisplayName("Resolution")]
        [ReadOnly(true)]
        [Browsable(true)]
        [DataMember]
        public Resolution Resolution { get; set; }

        [Category("Capital")]
        [DisplayName("Initial capial")]
        [ReadOnly(true)]
        [Browsable(true)]
        [DataMember]
        public long InitialCapital { get; set; }

        [Category("Capital")]
        [DisplayName("Percent capital per position")]
        [ReadOnly(true)]
        [Browsable(true)]
        [DataMember]
        public double PcntCapitalPerPosition { get; set; }

        [Category("Algorithm")]
        [DisplayName("File location")]
        [Editor(typeof(FilenameEditor), typeof(FilenameEditor))]
        [ReadOnly(true)]
        [Browsable(true)]
        [DataMember]
        public string AlgorithmLocation { get; set; }

        [Category("Algorithm")]
        [DisplayName("Algorithm name")]
        [ReadOnly(true)]
        [Browsable(true)]
        [DataMember]
        public string AlgorithmName { get; set; }

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public bool Active { get; set; } = true;

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public List<SymbolModel> Symbols { get; } = new List<SymbolModel>();

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public List<ParameterModel> Parameters { get; } = new List<ParameterModel>();

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public string DataFolder { get; set; }

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public string ApiUser { get; set; }

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public string ApiToken { get; set; }

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public bool ApiDownload { get; set; }

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public bool Completed { get; set; }

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public string Result { get; set; }

        [ReadOnly(true)]
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

            // Clone symbols
            Symbols.AddRange(strategy.Symbols.Select(m => new SymbolModel(m)));

            // Clone parameters
            Parameters.AddRange(strategy.Parameters.Select(m => new ParameterModel(m)));

            // Use paramerter list as job name
            string parameters = string.Join(" ", Parameters.Where(m => m.UseValue).Select(m => m.Value));
            if (!string.IsNullOrWhiteSpace(parameters))
            {
                Name = parameters;
            }
        }

        public void Refresh()
        {
        }
    }
}
