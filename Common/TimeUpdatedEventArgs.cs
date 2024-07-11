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
using NodaTime;

namespace QuantConnect
{
    /// <summary>
    /// Event arguments class for the <see cref="LocalTimeKeeper.TimeUpdated"/> event
    /// </summary>
    public sealed class TimeUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the new time
        /// </summary>
        public DateTime Time { get; init; }

        /// <summary>
        /// Gets the time zone
        /// </summary>
        public DateTimeZone TimeZone { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeUpdatedEventArgs"/> class
        /// </summary>
        /// <param name="time">The newly updated time</param>
        /// <param name="timeZone">The time zone of the new time</param>
        public TimeUpdatedEventArgs(DateTime time, DateTimeZone timeZone)
        {
            Time = time;
            TimeZone = timeZone;
        }
    }
}
