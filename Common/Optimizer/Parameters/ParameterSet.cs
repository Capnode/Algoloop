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

using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Api;
using QuantConnect.Util;
using System.Collections.Generic;

namespace QuantConnect.Optimizer.Parameters
{
    /// <summary>
    /// Represents a single combination of optimization parameters
    /// </summary>
    [JsonConverter(typeof(ParameterSetJsonConverter))]
    public class ParameterSet
    {
        /// <summary>
        /// The unique identifier within scope (current optimization job)
        /// </summary>
        /// <remarks>Internal id, useful for the optimization strategy to id each generated parameter sets,
        /// even before there is any backtest id</remarks>
        [JsonProperty(PropertyName = "id")]
        public int Id { get; }

        /// <summary>
        /// Represent a combination as key value of parameters, i.e. order doesn't matter
        /// </summary>
        [JsonProperty(PropertyName = "value", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyDictionary<string, string> Value { get; }

        /// <summary>
        /// Creates an instance of <see cref="ParameterSet"/> based on new combination of optimization parameters
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="value">Combination of optimization parameters</param>
        public ParameterSet(int id, IReadOnlyDictionary<string, string> value)
        {
            Id = id;
            Value = value;
        }

        /// <summary>
        /// String representation of this parameter set
        /// </summary>
        public override string ToString()
        {
            return string.Join(',', Value.OrderBy(kvp => kvp.Key).Select(arg => $"{arg.Key}:{arg.Value}"));
        }
    }
}
