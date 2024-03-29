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

namespace Borsdata.Api.Dal.Model
{
    public partial class MarketV1
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public long? CountryId { get; set; }
        public bool? IsIndex { get; set; }
        public string ExchangeName { get; set; }
    }
}
