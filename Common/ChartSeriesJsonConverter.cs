/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * Modifications Copyright (C) 2024 Capnode AB
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

using System;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect
{
    /// <summary>
    /// Convert a Chart Series to and from JSON
    /// </summary>
    public class ChartSeriesJsonConverter : JsonConverter
    {
        /// <summary>
        /// This converter wont be used to read JSON. Will throw exception if manually called.
        /// </summary>
        public override bool CanRead => false;

        /// <summary>
        /// Indicates whether the given object type can be converted into Chart Series
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Dictionary<string, BaseSeries>).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Converts a Chart Series object into a JSON file
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var series = value as Dictionary<string, BaseSeries>;
            if (series == null)
            {
                return;
            }

            writer.WriteStartObject();
            // we sort the series in ascending count so that they are chart nicely, has value for stacked area series so they're continuous 
            foreach (var kvp in series.OrderBy(x => x.Value.Index))
            {
                writer.WritePropertyName(kvp.Key);
                writer.WriteRawValue(JsonConvert.SerializeObject(kvp.Value));
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Converts a JSON file into a Chart Series object
        /// </summary>
        /// <remarks>Throws NotImplementedException</remarks>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
