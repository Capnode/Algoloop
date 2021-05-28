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

using Algoloop.Support;
using QuantConnect;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [Serializable]
    [DataContract]
    public class TrackModel : ModelBase
    {
        public enum CompletionStatus { None, Success, Error } ;

        private string _account;

        public TrackModel()
        {
            Symbols = new Collection<SymbolModel>();
            Parameters = new Collection<ParameterModel>();
        }

        public TrackModel(string name, StrategyModel strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));

            Name = name;
            Desktop = strategy.Desktop;
            Account = strategy.Account;
            Market = strategy.Market;
            Security = strategy.Security;
            BarsBack = strategy.BarsBack;
            StartDate = strategy.StartDate;
            EndDate = strategy.EndDate;
            InitialCapital = strategy.InitialCapital;
            PcntCapitalPerPosition = strategy.PcntCapitalPerPosition;
            AlgorithmLocation = strategy.AlgorithmLocation;
            AlgorithmName = strategy.AlgorithmName;
            AlgorithmLanguage = strategy.AlgorithmLanguage;
            Resolution = strategy.Resolution;

            // Clone symbols
            Symbols = new Collection<SymbolModel>(strategy.Symbols.Select(m => new SymbolModel(m)).ToList());

            // Clone parameters
            Parameters = new Collection<ParameterModel>(strategy.Parameters.Select(m => new ParameterModel(m)).ToList());

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
            }
        }

        [Category("Data")]
        [DisplayName("Market")]
        [Description("Market data for backtest. Must match market folder in data folder structure.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string Market { get; set; }

        [Category("Data")]
        [DisplayName("Security type")]
        [Description("Asset security type. Must match security folder in data folder structure.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public SecurityType Security { get; set; }

        [Category("Data")]
        [DisplayName("Bars back")]
        [Description("Number of bars to backtest")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public int BarsBack { get; set; }

        [Category("Data")]
        [DisplayName("Date from")]
        [Description("Backtest from date")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Category("Data")]
        [DisplayName("Date to")]
        [Description("Backtest to date")]
        [Browsable(true)]
        [ReadOnly(true)]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        [DataMember]
        public DateTime EndDate { get; set; } = DateTime.Today;

        [Category("Data")]
        [DisplayName("Resolution")]
        [Description("Period resolution. Must match resolution folder in data folder structure.")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public Resolution Resolution { get; set; }

        [Category("Capital")]
        [DisplayName("Initial capial")]
        [Description("Start capital")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public long InitialCapital { get; set; }

        [Category("Capital")]
        [DisplayName("Percent capital per position")]
        [Description("Capital used for each position")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public double PcntCapitalPerPosition { get; set; }

        [Category("Algorithm")]
        [DisplayName("File location")]
        [Description("Algorithm file location")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string AlgorithmLocation { get; set; }

        [Category("Algorithm")]
        [DisplayName("Algorithm name")]
        [Description("Name of algorithm")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public string AlgorithmName { get; set; }

        [Category("Algorithm")]
        [DisplayName("Algorithm language")]
        [Description("Programming language of algorithm")]
        [Browsable(true)]
        [ReadOnly(true)]
        [DataMember]
        public Language AlgorithmLanguage { get; set; }


        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public bool Active { get; set; } = true;

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public Collection<SymbolModel> Symbols { get; }

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public Collection<ParameterModel> Parameters { get; }

        [ReadOnly(true)]
        [Browsable(false)]
        [DataMember]
        public CompletionStatus Status { get; set; }

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
    }
}
