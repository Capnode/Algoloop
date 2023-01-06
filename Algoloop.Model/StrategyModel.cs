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
    [CategoryOrder("Algorithm", 3)]
    [CategoryOrder("Data", 4)]
    [DataContract]
    public class StrategyModel : ModelBase
    {
        private string _name;
        private Language _algorithmLanguage;
        private string _algorithmLocation;
        private string _algorithmFolder;
        private string _algorithmName;
        private string _account = AccountModel.AccountType.Backtest.ToString();
        private bool _isDataValid = true;

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
            Account = model.Account;
            AlgorithmLanguage = model.AlgorithmLanguage;
            AlgorithmFolder = model.AlgorithmFolder;
            AlgorithmLocation = model.AlgorithmLocation;
            AlgorithmName = model.AlgorithmName;

            IsDataValid = model.IsDataValid;
            Market = model.Market;
            Security = model.Security;
            BarsBack = model.BarsBack;
            StartDate = model.StartDate;
            EndDate = model.EndDate;
            Resolution = model.Resolution;
            InitialCapital = model.InitialCapital;
            PcntCapitalPerPosition = model.PcntCapitalPerPosition;

            Symbols = new Collection<SymbolModel>(model.Symbols.Select(m => new SymbolModel(m)).ToList());
            Parameters = new Collection<ParameterModel>(model.Parameters.Select(m => new ParameterModel(m)).ToList());
            Tracks = new Collection<TrackModel>();
            Strategies = new Collection<StrategyModel>();
        }

        public StrategyModel(TrackModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            Account = model.Account;
            AlgorithmLanguage = model.AlgorithmLanguage;
            AlgorithmLocation = model.AlgorithmLocation;
            AlgorithmName = model.AlgorithmName;

            IsDataValid = model.IsDataValid;
            Market = model.Market;
            Security = model.Security;
            BarsBack = model.BarsBack;
            StartDate = model.StartDate;
            EndDate = model.EndDate;
            Resolution = model.Resolution;
            InitialCapital = model.InitialCapital;
            PcntCapitalPerPosition = model.PcntCapitalPerPosition;

            Symbols = new Collection<SymbolModel>(model.Symbols.Select(m => new SymbolModel(m)).ToList());
            Parameters = new Collection<ParameterModel>(model.Parameters.Select(m => new ParameterModel(m) { UseRange = false }).ToList());
            Tracks = new Collection<TrackModel>();
            Strategies = new Collection<StrategyModel>();
        }

        [Browsable(false)]
        public Action NameChanged { get; set; }

        [Browsable(false)]
        public Action AlgorithmNameChanged { get; set; }

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
        [Description("Folder path to algorithm.")]
        [Editor(typeof(FolderEditor), typeof(FolderEditor))]
        [RefreshProperties(RefreshProperties.All)]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string AlgorithmFolder
        {
            get => _algorithmFolder;
            set
            {
                _algorithmFolder = value;
                _algorithmName = null;
                Refresh();
            }
        }

        [Category("Algorithm")]
        [PropertyOrder(3)]
        [DisplayName("Algorithm location")]
        [Description("File path to algorithm.")]
        [Editor(typeof(FilenameEditor), typeof(FilenameEditor))]
        [RefreshProperties(RefreshProperties.All)]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string AlgorithmLocation
        {
            get => _algorithmLocation;
            set => _algorithmLocation = value;
        }

        [Category("Algorithm")]
        [PropertyOrder(4)]
        [DisplayName("Algorithm name")]
        [Description("Name of algorithm.")]
        [TypeConverter(typeof(AlgorithmNameConverter))]
        [RefreshProperties(RefreshProperties.Repaint)]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string AlgorithmName
        {
            get => _algorithmName;
            set
            {
                _algorithmName = value;
                Refresh();
                AlgorithmNameChanged?.Invoke();
            }
        }

        [Category("Data")]
        [PropertyOrder(1)]
        [DisplayName("Set strategy data")]
        [Description("Set user defined strategy data, symbols and parameters.")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [TypeConverter(typeof(ProviderNameConverter))]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public bool IsDataValid
        {
            get => _isDataValid;
            set
            {
                _isDataValid = value;
                Refresh();
            }
        }

        [Category("Data")]
        [PropertyOrder(2)]
        [DisplayName("Market")]
        [Description("Market data for backtest. Must match market folder in data folder structure.")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [TypeConverter(typeof(ProviderNameConverter))]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public string Market { get; set; }

        [Category("Data")]
        [PropertyOrder(3)]
        [DisplayName("Security type")]
        [Description("Asset security type. Must match security folder in data folder structure.")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public SecurityType Security { get; set; } = SecurityType.Equity;

        [Category("Data")]
        [PropertyOrder(4)]
        [DisplayName("Bars back")]
        [Description("Number of bars to backtest")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public int BarsBack { get; set; }

        [Category("Data")]
        [PropertyOrder(5)]
        [DisplayName("Date from")]
        [Description("Backtest from date")]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Category("Data")]
        [PropertyOrder(6)]
        [DisplayName("Date to")]
        [Description("Backtest to date")]
        [Editor(typeof(DateEditor), typeof(DateEditor))]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public DateTime EndDate { get; set; } = DateTime.Today;

        [Category("Data")]
        [PropertyOrder(7)]
        [DisplayName("Resolution")]
        [Description("Period resolution. Must match resolution folder in data folder structure.")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public Resolution Resolution { get; set; } = Resolution.Daily;

        [Category("Data")]
        [PropertyOrder(8)]
        [DisplayName("Initial capial")]
        [Description("Start capital")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public long InitialCapital { get; set; }

        [Category("Data")]
        [PropertyOrder(9)]
        [DisplayName("Percent capital per position")]
        [Description("Capital used for each position")]
        [Browsable(true)]
        [ReadOnly(false)]
        [DataMember]
        public double PcntCapitalPerPosition { get; set; }

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

            if (AlgorithmLanguage == Language.Python)
            {
                SetReadonly(nameof(AlgorithmLocation), true);
                SetBrowsable(nameof(AlgorithmFolder), true);
                if (string.IsNullOrEmpty(AlgorithmFolder) || string.IsNullOrEmpty(AlgorithmName))
                {
                    AlgorithmLocation = null;
                }
                else
                {
                    AlgorithmLocation = Path.Combine(AlgorithmFolder, AlgorithmName + ".py");
                }
            }
            else
            {
                SetReadonly(nameof(AlgorithmLocation), false);
                SetBrowsable(nameof(AlgorithmFolder), false);
            }

            bool visible = IsDataValid;
            SetBrowsable(nameof(Market), visible);
            SetBrowsable(nameof(Security), visible);
            SetBrowsable(nameof(BarsBack), visible);
            SetBrowsable(nameof(StartDate), visible);
            SetBrowsable(nameof(EndDate), visible);
            SetBrowsable(nameof(Resolution), visible);
            SetBrowsable(nameof(InitialCapital), visible);
            SetBrowsable(nameof(PcntCapitalPerPosition), visible);
        }
    }
}
