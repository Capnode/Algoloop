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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [DataContract]
    public class StrategyModel : ModelBase
    {
        private string _name;
        private string _algorithmLocation;
        private string _algorithmName;
        private string _account = AccountModel.AccountType.Backtest.ToString();

        public StrategyModel()
        {
            Symbols = new Collection<SymbolModel>();
            Parameters = new Collection<ParameterModel>();
            Tracks = new Collection<TrackModel>();
        }

        public StrategyModel(StrategyModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            Name = model.Name;
            Desktop = model.Desktop;
            Market = model.Market;
            Security = model.Security;
            Account = model.Account;
            BarsBack = model.BarsBack;
            StartDate = model.StartDate;
            EndDate = model.EndDate;
            Resolution = model.Resolution;
            InitialCapital = model.InitialCapital;
            PcntCapitalPerPosition = model.PcntCapitalPerPosition;
            AlgorithmLocation = model.AlgorithmLocation;
            AlgorithmName = model.AlgorithmName;
            AlgorithmLanguage = model.AlgorithmLanguage;

            Symbols = new Collection<SymbolModel>(model.Symbols.Select(m => new SymbolModel(m)).ToList());
            Parameters = new Collection<ParameterModel>(model.Parameters.Select(m => new ParameterModel(m)).ToList());
            Tracks = new Collection<TrackModel>();
        }

        public StrategyModel(TrackModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            Desktop = model.Desktop;
            Market = model.Market;
            Security = model.Security;
            Account = model.Account;
            BarsBack = model.BarsBack;
            StartDate = model.StartDate;
            EndDate = model.EndDate;
            Resolution = model.Resolution;
            InitialCapital = model.InitialCapital;
            PcntCapitalPerPosition = model.PcntCapitalPerPosition;
            AlgorithmLocation = model.AlgorithmLocation;
            AlgorithmName = model.AlgorithmName;
            AlgorithmLanguage = model.AlgorithmLanguage;

            Symbols = new Collection<SymbolModel>(model.Symbols.Select(m => new SymbolModel(m)).ToList());
            Parameters = new Collection<ParameterModel>(model.Parameters.Select(m => new ParameterModel(m) { UseRange = false }).ToList());
            Tracks = new Collection<TrackModel>();
        }

        [Browsable(false)]
        public Action NameChanged { get; set; }
        [Browsable(false)]

        public Action<string> AlgorithmNameChanged { get; set; }

        [Category("Information")]
        [DisplayName("Name")]
        [Description("Name of the strategy.")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NameChanged?.Invoke();
            }
        }

        [Category("Information")]
        [DisplayName("Desktop")]
        [Description("Desktop execution.")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public bool Desktop { get; set; }

        [Category("Broker")]
        [DisplayName("Account")]
        [Description("Name of trading account.")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [TypeConverter(typeof(AccountNameConverter))]
        [Browsable(true)]
        [ReadOnly(false)]
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

        [Category("Data")]
        [DisplayName("Market")]
        [Description("Market data for backtest. Must match market folder in data folder structure.")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [TypeConverter(typeof(ProviderNameConverter))]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string Market { get; set; }

        [Category("Data")]
        [DisplayName("Security type")]
        [Description("Asset security type. Must match security folder in data folder structure.")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public SecurityType Security { get; set; }

        [Category("Data")]
        [DisplayName("Bars back")]
        [Description("Number of bars to backtest")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public int BarsBack { get; set; }

        [Category("Data")]
        [DisplayName("Date from")]
        [Description("Backtest from date")]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Category("Data")]
        [DisplayName("Date to")]
        [Description("Backtest to date")]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public DateTime EndDate { get; set; } = DateTime.Today;

        [Category("Data")]
        [DisplayName("Resolution")]
        [Description("Period resolution. Must match resolution folder in data folder structure.")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public Resolution Resolution { get; set; } = Resolution.Daily;

        [Category("Capital")]
        [DisplayName("Initial capial")]
        [Description("Start capital")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public long InitialCapital { get; set; }

        [Category("Capital")]
        [DisplayName("Percent capital per position")]
        [Description("Capital used for each position")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public double PcntCapitalPerPosition { get; set; }

        [Category("Algorithm")]
        [DisplayName("File location")]
        [Description("Algorithm file location")]
        [Editor(typeof(FilenameEditor), typeof(FilenameEditor))]
        [RefreshProperties(RefreshProperties.All)]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string AlgorithmLocation
        {
            get { return _algorithmLocation; }
            set
            {
                _algorithmLocation = value;
                AlgorithmNameChanged?.Invoke(_algorithmName);
            }
        }

        [Category("Algorithm")]
        [DisplayName("Algorithm name")]
        [Description("Name of algorithm")]
        [TypeConverter(typeof(AlgorithmNameConverter))]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string AlgorithmName
        {
            get { return _algorithmName; }
            set
            {
                _algorithmName = value;
                AlgorithmNameChanged?.Invoke(_algorithmName);
            }
        }

        [Category("Algorithm")]
        [DisplayName("Algorithm language")]
        [Description("Programming language of algorithm")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public Language AlgorithmLanguage { get; set; }

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<SymbolModel> Symbols { get; }

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<ParameterModel> Parameters { get; }

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<TrackModel> Tracks { get; }

        public void Refresh()
        {
            if (Account == null
             || Account.Equals(AccountModel.AccountType.Backtest.ToString(), StringComparison.OrdinalIgnoreCase)
             || Account.Equals(AccountModel.AccountType.Paper.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                SetBrowsable(nameof(Market), true);
            }
            else
            {
                SetBrowsable(nameof(Market), false);
            }
        }
    }
}
