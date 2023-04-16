using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Borsdata.Api.Dal.Infrastructure
{
    // Enum doesn't support some words like default so I use class as workaround
    public class CalcType
    {
        public static string high { get { return "high"; } }
        public static string latest { get { return "latest"; } }
        public static string low { get { return "low"; } }
        public static string mean { get { return "mean"; } }
        public static string sum { get { return "sum"; } }
        public static string cagr { get { return "cagr"; } }
        public static string _default { get { return "default"; } }
        public static string _return { get { return "return"; } }
        public static string growth { get { return "growth"; } }
        public static string diff { get { return "diff"; } }
        public static string trend { get { return "trend"; } }
        public static string over { get { return "over"; } }
        public static string under { get { return "under"; } }
        public static string rank { get { return "rank"; } }
        public static string point { get { return "point"; } }
        public static string quarter { get { return "quarter"; } }
        public static string stabil { get { return "stabil"; } }

    }
}
