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
        private string _account;

        [Category("Information")]
        [DisplayName("Name")]
        [Description("Name of the job.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string Name { get; set; } = "Job";

        [Category("Information")]
        [DisplayName("Desktop")]
        [Description("Desktop execution.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public bool Desktop { get; set; }

        [Category("Broker")]
        [DisplayName("Data provider")]
        [Description("Market data provider")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public MarketType Provider { get; set; }

        [Category("Broker")]
        [DisplayName("Account")]
        [Description("Trading account for live or paper trading.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string Account
        {
            get => _account;
            set
            {
                _account = value;
                Refresh();
            }
        }

        [Category("Time")]
        [DisplayName("Bars back")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public int BarsBack { get; set; }

        [Category("Time")]
        [DisplayName("From date")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Category("Time")]
        [DisplayName("To date")]
        [Browsable(true)]
        [ReadOnly(true)]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        [DataMember]
        public DateTime EndDate { get; set; } = DateTime.Today;

        [Category("Time")]
        [DisplayName("Resolution")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public Resolution Resolution { get; set; }

        [Category("Capital")]
        [DisplayName("Initial capial")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public long InitialCapital { get; set; }

        [Category("Capital")]
        [DisplayName("Percent capital per position")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public double PcntCapitalPerPosition { get; set; }

        [Category("Algorithm")]
        [DisplayName("File location")]
        [Editor(typeof(FilenameEditor), typeof(FilenameEditor))]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string AlgorithmLocation { get; set; }

        [Category("Algorithm")]
        [DisplayName("Algorithm name")]
        [Browsable(true)]
        [ReadOnly(true)]
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
            Desktop = strategy.Desktop;
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
            if (Account == null
             || Account.Equals(AccountModel.AccountType.Backtest.ToString())
             || Account.Equals(AccountModel.AccountType.Paper.ToString()))
            {
                SetBrowsable("Provider", true);
            }
            else
            {
                SetBrowsable("Provider", false);
            }
        }
    }
}
