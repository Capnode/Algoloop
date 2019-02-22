

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

    public partial class KpiHistoryV1
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KpiHistoryV1" /> class.
        /// </summary>
        /// <param name="y">Year.</param>
        /// <param name="p">Period.</param>
        /// <param name="d">Date.</param>
        /// <param name="v">Value.</param>
        public KpiHistoryV1(int? y = default(int?), int? p = default(int?), DateTime? d = default(DateTime?), double? v = default(double?))
        {
            this.Y = y;
            this.P = p;
            this.D = d;
            this.V = v;
        }
        
        /// <summary>
        /// Year
        /// </summary>
        /// <value>Year</value>
        public int? Y { get; set; }

        /// <summary>
        /// Period
        /// </summary>
        /// <value>Period</value>
        public int? P { get; set; }

        /// <summary>
        /// Date
        /// </summary>
        /// <value>Date</value>
        public DateTime? D { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        /// <value>Value</value>
        public double? V { get; set; }

        
    }

}
