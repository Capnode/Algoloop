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
 *
*/
using System;
using System.Collections.Generic;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Transport type for algorithm update data. This is intended to provide a
    /// list of base data used to perform updates against the specified target
    /// </summary>
    /// <typeparam name="T">The target type</typeparam>
    public class UpdateData<T>
    {
        /// <summary>
        /// Flag indicating whether <see cref="Data"/> contains any fill forward bar or not
        /// </summary>
        /// <remarks>This is useful for performance, it allows consumers to skip re enumerating the entire data
        /// list to filter any fill forward data</remarks>
        public bool? ContainsFillForwardData { get; init; }

        /// <summary>
        /// The target, such as a security or subscription data config
        /// </summary>
        public T Target { get; init; }

        /// <summary>
        /// The data used to update the target
        /// </summary>
        public IReadOnlyList<BaseData> Data { get; init; }

        /// <summary>
        /// The type of data in the data list
        /// </summary>
        public Type DataType { get; init; }

        /// <summary>
        /// True if this update data corresponds to an internal subscription
        /// such as currency or security benchmark
        /// </summary>
        public bool IsInternalConfig { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateData{T}"/> class
        /// </summary>
        /// <param name="target">The end consumer/user of the dat</param>
        /// <param name="dataType">The type of data in the list</param>
        /// <param name="data">The update data</param>
        /// <param name="isInternalConfig">True if this update data corresponds to an internal subscription
        /// such as currency or security benchmark</param>
        /// <param name="containsFillForwardData">True if this update data contains fill forward bars</param>
        public UpdateData(T target, Type dataType, IReadOnlyList<BaseData> data, bool isInternalConfig, bool? containsFillForwardData = null)
        {
            Target = target;
            Data = data;
            DataType = dataType;
            IsInternalConfig = isInternalConfig;
            ContainsFillForwardData = containsFillForwardData;
        }
    }
}
