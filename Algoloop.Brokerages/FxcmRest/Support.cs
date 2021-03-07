/*
 * Copyright 2021 Capnode AB
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Newtonsoft.Json.Linq;
using QuantConnect;
using System;
using System.Globalization;

namespace Algoloop.Brokerages.FxcmRest
{
    public static class Support
    {
        public const string Market = "fxcm";

        private static readonly DateTime _epochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        public static SecurityType ToSecurityType(string symbol)
        {
            if (symbol == default) throw new ArgumentNullException(nameof(symbol));
            return SecurityType.Forex;
        }

        public static SecurityType ToSecurityType(int number)
        {
            switch (number)
            {
                case 1: return SecurityType.Forex; // Forex
                case 2: return SecurityType.Cfd; // Indices
                case 3: return SecurityType.Cfd; // Commodity
                case 4: return SecurityType.Cfd; // Treasury
                case 5: return SecurityType.Cfd; // Bullion
                case 6: return SecurityType.Cfd; // Shares
                case 7: return SecurityType.Forex; // FX index
                default: return SecurityType.Cfd; // Undefined
            }
        }

        public static DateTime ToTime(string time)
        {
            return DateTime.ParseExact(time, "MMddyyyyHHmmss", CultureInfo.InvariantCulture);
        }

        public static DateTime ToTime(long time)
        {
            return _epochTime.AddTicks(time * TimeSpan.TicksPerMillisecond);
        }

        public static decimal ToDecimal(this JToken jToken)
        {
            if (jToken.Type == JTokenType.Null) return 0;
            return (decimal)jToken;
        }
    }
}
