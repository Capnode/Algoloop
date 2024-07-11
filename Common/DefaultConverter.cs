/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
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
using Newtonsoft.Json;

namespace QuantConnect
{
    /// <summary>
    /// Helper json converter to use the default json converter, breaking inheritance json converter
    /// </summary>
    public class DefaultConverter : JsonConverter
    {
        /// <summary>
        /// Indicates if this object can be read
        /// </summary>
        public override bool CanRead => false;

        /// <summary>
        /// Indicates if this object can be written
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Writes a JSON file from the given object and the other arguments
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an object from a given JSON reader and other arguments
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Indicates if the given type can be assigned to this object
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }
}
