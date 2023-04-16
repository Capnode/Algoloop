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
    public partial class StockPriceV1
    {
        /// <summary>
        /// Date
        /// </summary>
        /// <value>Date</value>
        [DataMember(Name="d", EmitDefaultValue=false)]
        public string D { get; set; }

        /// <summary>
        /// Highest Price
        /// </summary>
        /// <value>Highest Price</value>
        [DataMember(Name="h", EmitDefaultValue=false)]
        public double? H { get; set; }

        /// <summary>
        /// Lowest Price
        /// </summary>
        /// <value>Lowest Price</value>
        [DataMember(Name="l", EmitDefaultValue=false)]
        public double? L { get; set; }

        /// <summary>
        /// Closing Price
        /// </summary>
        /// <value>Closing Price</value>
        [DataMember(Name="c", EmitDefaultValue=false)]
        public double? C { get; set; }

        /// <summary>
        /// Opening Price
        /// </summary>
        /// <value>Opening Price</value>
        [DataMember(Name="o", EmitDefaultValue=false)]
        public double? O { get; set; }

        /// <summary>
        /// Total Volume
        /// </summary>
        /// <value>Total Volume</value>
        [DataMember(Name="v", EmitDefaultValue=false)]
        public long? V { get; set; }
    }
}
