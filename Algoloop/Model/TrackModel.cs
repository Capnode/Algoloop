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

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class TrackModel : ModelBase
    {
        private string _account;

        public TrackModel()
        {
        }

        public TrackModel(string name, StrategyModel strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));

            Name = name;
            Desktop = strategy.Desktop;
            Account = strategy.Account;
            Market = strategy.Market;
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

            // Use paramerter list as track name
            string parameters = string.Join(" ", Parameters.Where(m => m.UseValue).Select(m => m.Value));
            if (!string.IsNullOrWhiteSpace(parameters))
            {
                Name = parameters;
            }
        }

        [Category("Information")]
        [DisplayName("Name")]
        [Description("Name of the track.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string Name { get; set; } = "Track";

        [Category("Information")]
        [DisplayName("Desktop")]
        [Description("Desktop execution.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public bool Desktop { get; set; }

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

        [Category("Broker")]
        [DisplayName("Market data")]
        [Description("Market data for backtest")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string Market { get; set; }

        [Category("Broker")]
        [DisplayName("Market provider")]
        [Description("Market data provider for backtest")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string Provider { get; set; }

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

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public string ZipFile { get; set; }

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public IDictionary<string, decimal?> Statistics { get; set; }

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
