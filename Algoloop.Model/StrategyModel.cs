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

using Algoloop.Model.Internal;
using QuantConnect;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Algoloop.Model
{
    [CategoryOrder("Strategy", 1)]
    [CategoryOrder("Broker", 2)]
    [CategoryOrder("Capital", 3)]
    [CategoryOrder("Algorithm", 4)]
    [CategoryOrder("Data", 5)]
    [DataContract]
    public class StrategyModel : ModelBase
    {
        private string _name;
        private Language _algorithmLanguage;
        private string _algorithmLocation;
        private string _algorithmFolder;
        private string _algorithmFile;
        private string _algorithmName;
        private string _account = AccountModel.AccountType.Backtest.ToString();

        public StrategyModel()
        {
            Symbols = new Collection<SymbolModel>();
            Parameters = new Collection<ParameterModel>();
            Tracks = new Collection<TrackModel>();
            Strategies = new Collection<StrategyModel>();
        }

        public StrategyModel(StrategyModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            Name = model.Name;
            Market = model.Market;
            Security = model.Security;
            Account = model.Account;
            BarsBack = model.BarsBack;
            StartDate = model.StartDate;
            EndDate = model.EndDate;
            Resolution = model.Resolution;
            InitialCapital = model.InitialCapital;
            PcntCapitalPerPosition = model.PcntCapitalPerPosition;
            AlgorithmLanguage = model.AlgorithmLanguage;
            AlgorithmFolder = model.AlgorithmFolder;
            AlgorithmFile = model.AlgorithmFile;
            AlgorithmName = model.AlgorithmName;

            Symbols = new Collection<SymbolModel>(model.Symbols.Select(m => new SymbolModel(m)).ToList());
            Parameters = new Collection<ParameterModel>(model.Parameters.Select(m => new ParameterModel(m)).ToList());
            Tracks = new Collection<TrackModel>();
            Strategies = new Collection<StrategyModel>();
        }

        public StrategyModel(TrackModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            Market = model.Market;
            Security = model.Security;
            Account = model.Account;
            BarsBack = model.BarsBack;
            StartDate = model.StartDate;
            EndDate = model.EndDate;
            Resolution = model.Resolution;
            InitialCapital = model.InitialCapital;
            PcntCapitalPerPosition = model.PcntCapitalPerPosition;
            AlgorithmLanguage = model.AlgorithmLanguage;
            AlgorithmFolder = model.AlgorithmFolder;
            AlgorithmFile = model.AlgorithmFile;
            AlgorithmName = model.AlgorithmName;

            Symbols = new Collection<SymbolModel>(model.Symbols.Select(m => new SymbolModel(m)).ToList());
            Parameters = new Collection<ParameterModel>(model.Parameters.Select(m => new ParameterModel(m) { UseRange = false }).ToList());
            Tracks = new Collection<TrackModel>();
            Strategies = new Collection<StrategyModel>();
        }

        [Browsable(false)]
        public Action NameChanged { get; set; }

        [Browsable(false)]
        public Action<string> AlgorithmNameChanged { get; set; }

        [Category("Strategy")]
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

        [Browsable(false)]
        [Obsolete]
        [DataMember]
        public string AlgorithmLocation
        {
            get => _algorithmLocation;
            set => _algorithmLocation = value;
        }

        [Category("Algorithm")]
        [PropertyOrder(1)]
        [DisplayName("Algorithm language")]
        [Description("Programming language of algorithm.")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public Language AlgorithmLanguage
        {
            get => _algorithmLanguage;
            set
            {
                _algorithmLanguage = value;
                Refresh();
            }
        }

        [Category("Algorithm")]
        [PropertyOrder(2)]
        [DisplayName("Algorithm folder")]
        [Description("Directory of algorithm files. Leave empty to use use install folder.")]
        [Editor(typeof(FolderEditor), typeof(FolderEditor))]
        [RefreshProperties(RefreshProperties.Repaint)]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string AlgorithmFolder
        {
            get => _algorithmFolder;
            set => _algorithmFolder = value;
        }

        [Category("Algorithm")]
        [PropertyOrder(3)]
        [DisplayName("Algorithm file")]
        [Description("File of algorithm.")]
        [TypeConverter(typeof(AlgorithmFileConverter))]
        [RefreshProperties(RefreshProperties.Repaint)]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string AlgorithmFile
        {
            get => _algorithmFile;
            set => _algorithmFile = value;
        }

        [Category("Algorithm")]
        [PropertyOrder(4)]
        [DisplayName("Algorithm name")]
        [Description("Name of algorithm.")]
        [TypeConverter(typeof(AlgorithmNameConverter))]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string AlgorithmName
        {
            get => _algorithmName;
            set
            {
                _algorithmName = value;
                AlgorithmNameChanged?.Invoke(_algorithmName);
            }
        }

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

        [Browsable(false)]
        [ReadOnly(false)]
        [DataMember]
        public Collection<StrategyModel> Strategies { get; }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            // Database upgrade
            if (_algorithmLocation != null)
            {
                AlgorithmFolder = Path.GetDirectoryName(_algorithmLocation);
                AlgorithmFile = Path.GetFileName(_algorithmLocation);
                _algorithmLocation = null;
            }
        }

        public void Refresh()
        {
            SetBrowsable(nameof(AlgorithmFolder), true);
            if (_algorithmLanguage.Equals(Language.CSharp))
            {
                SetBrowsable(nameof(AlgorithmFolder), false);
            }

            SetBrowsable(nameof(Market), false);
            if (Account == null
             || Account.Equals(AccountModel.AccountType.Backtest.ToString(), StringComparison.OrdinalIgnoreCase)
             || Account.Equals(AccountModel.AccountType.Paper.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                SetBrowsable(nameof(Market), true);
            }
        }
    }
}
