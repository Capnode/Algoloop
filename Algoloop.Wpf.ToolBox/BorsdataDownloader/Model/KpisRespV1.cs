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
    public partial class KpisRespV1
    {
        public int? KpiId { get; set; }
        public string Group { get; set; }
        public string Calculation { get; set; }
        public KpiV1 Value { get; set; }
    }
}
