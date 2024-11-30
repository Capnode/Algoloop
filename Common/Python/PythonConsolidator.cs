﻿/*
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

using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Python
{
    /// <summary>
    /// Provides a base class for python consolidators, necessary to use event handler.
    /// </summary>
    public class PythonConsolidator
    {
        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event DataConsolidatedHandler DataConsolidated;

        /// <summary>
        /// Function to invoke the event handler
        /// </summary>
        /// <param name="consolidator">Reference to the consolidator itself</param>
        /// <param name="data">The finished data from the consolidator</param>
        public void OnDataConsolidated(PyObject consolidator, IBaseData data)
        {
            DataConsolidated?.Invoke(consolidator, data);
        }

        /// <summary>
        /// Resets the consolidator
        /// </summary>
        public virtual void Reset()
        {
        }
    }
}
