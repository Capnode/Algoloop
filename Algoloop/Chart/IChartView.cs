/*
 * Copyright 2018 Capnode AB
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
using System.Collections.Generic;

namespace Algoloop.Charts
{
    public interface IChartView : IChartParser
    {
        /// <summary>
        /// Gets the dictionary containing the Last Update instant for a specific series
        /// </summary>§
        Dictionary<string, DateTime> LastUpdates { get; }

        /// <summary>
        /// Gets the axismodifier which can be used to convert X Axis values back to their actual Instant
        /// </summary>
        double AxisModifier { get; }

        /// <summary>
        /// Gets or sets whether the view position of the chart is locked
        /// </summary>
        /// <remarks>When unlocked, the chart wil automatically scroll to new updates</remarks>
        bool IsPositionLocked { get; set;  }
    }
}