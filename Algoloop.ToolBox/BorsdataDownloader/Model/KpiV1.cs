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
    public partial class KpiV1
    {
        /// <summary>
        /// Instrument Id
        /// </summary>
        /// <value>Instrument Id</value>
        public long? I { get; set; }

        /// <summary>
        /// Numeric Value
        /// </summary>
        /// <value>Numeric Value</value>
        public double? N { get; set; }

        /// <summary>
        /// String Value
        /// </summary>
        /// <value>String Value</value>
        public string S { get; set; }
    }
}
