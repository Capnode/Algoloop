using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Borsdata.Api.Dal.Infrastructure
{
    public class TimeType
    {
        public static string last { get { return "last"; } }

        public static string _1week { get { return "1week"; } }
        public static string _1day { get { return "1day"; } }
        public static string _3day { get { return "3day"; } }
        public static string _5day { get { return "5day"; } }
        public static string _7day { get { return "7day"; } }
        public static string _10day { get { return "10day"; } }
        public static string _20day { get { return "20day"; } }
        public static string _30day { get { return "30day"; } }
        public static string _50day { get { return "50day"; } }
        public static string _70day { get { return "70day"; } }
        public static string _100day { get { return "100day"; } }
        public static string _200day { get { return "200day"; } }

        public static string _1month { get { return "1month"; } }
        public static string _3month { get { return "3month"; } }
        public static string _6month { get { return "6month"; } }

        public static string _1year { get { return "1year"; } }
        public static string _3year { get { return "3year"; } }
        public static string _5year { get { return "5year"; } }
        public static string _7year { get { return "7year"; } }
        public static string _10year { get { return "10year"; } }
        public static string _15year { get { return "15year"; } }

        public static string ma20ma50 { get { return "ma20ma50"; } }
        public static string ma20ma70 { get { return "ma20ma70"; } }
        public static string ma50ma200 { get { return "ma50ma200"; } }
        public static string ma5ma20 { get { return "ma5ma20"; } }
    }
}
