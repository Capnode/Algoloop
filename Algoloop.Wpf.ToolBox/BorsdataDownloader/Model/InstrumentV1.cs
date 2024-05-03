using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Borsdata.Api.Dal.Infrastructure;

namespace Borsdata.Api.Dal.Model
{
    public partial class InstrumentV1
    {
        public long? InsId { get; set; }
        public string Name { get; set; }
        public string UrlName { get; set; }
        public Instrument? Instrument { get; set; }
        public string Isin { get; set; }
        public string Ticker { get; set; }
        public string Yahoo { get; set; }
        public long? SectorId { get; set; }
        public long? MarketId { get; set; }
        public long? BranchId { get; set; }
        public long? CountryId { get; set; }
        public DateTime? ListingDate { get; set; }

        [JsonIgnore]
        public SectorV1 SectorModel { get; set; }

        [JsonIgnore]
        public MarketV1 MarketModel { get; set; }

        [JsonIgnore]
        public CountryV1 CountryModel { get; set; }

        [JsonIgnore]
        public BranchV1 BranchModel { get; set; }
    }
}
