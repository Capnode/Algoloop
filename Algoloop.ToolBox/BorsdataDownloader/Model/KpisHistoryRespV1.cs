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
    public partial class KpisHistoryRespV1
    {
        public int? KpiId { get; set; }
        public string ReportTime { get; set; }
        public string PriceValue { get; set; }
        public List<KpiHistoryV1> Values { get; set; }
    }
}
