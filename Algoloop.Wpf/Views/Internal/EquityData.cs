using StockSharp.Xaml.Charting;
using System;
using System.Runtime.Serialization;

namespace Algoloop.Wpf.Views.Internal
{
    //
    // Summary:
    //     Equity data.
    [DataContract]
    internal class EquityData : LineData<DateTime>
    {
        //
        // Summary:
        //     The time stamp in which the equity value was equal to StockSharp.Xaml.Charting.EquityData.Value.
        [DataMember]
        public DateTimeOffset Time { get; set; }

        //
        // Summary:
        //     The equity value.
        [DataMember]
        public decimal Value { get; set; }
    }
}
