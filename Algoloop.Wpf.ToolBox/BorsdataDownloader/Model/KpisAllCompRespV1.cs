using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Borsdata.Api.Dal.Model
{
    public partial class KpisAllCompRespV1
    {
        public int? KpiId { get; set; }
        public string Group { get; set; }
        public string Calculation { get; set; }
        public List<KpiV1> Values { get; set; }
    }
}
