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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Defines the different types of moving averages
    /// </summary>  
    public enum MovingAverageType
    {
        /// <summary>
        /// An unweighted, arithmetic mean (0)
        /// </summary>
        Simple,
        /// <summary>
        /// The standard exponential moving average, using a smoothing factor of 2/(n+1) (1)
        /// </summary>
        Exponential,
        /// <summary>
        /// An exponential moving average, using a smoothing factor of 1/n and simple moving average as seeding (2)
        /// </summary>
        Wilders,
        /// <summary>
        /// A weighted moving average type (3)
        /// </summary>
        LinearWeightedMovingAverage,
        /// <summary>
        /// The double exponential moving average (4)
        /// </summary>
        DoubleExponential,
        /// <summary>
        /// The triple exponential moving average (5)
        /// </summary>
        TripleExponential,
        /// <summary>
        /// The triangular moving average (6)
        /// </summary>
        Triangular,
        /// <summary>
        /// The T3 moving average (7)
        /// </summary>
        T3,
        /// <summary>
        /// The Kaufman Adaptive Moving Average (8)
        /// </summary>
        Kama,
        /// <summary>
        /// The Hull Moving Average (9)
        /// </summary>
        Hull,
        /// <summary>
        /// The Arnaud Legoux Moving Average (10)
        /// </summary>
        Alma,
        /// <summary>
        /// The Zero Lag Exponential Moving Average (11)
        /// </summary>
        Zlema,
        /// <summary>
        /// The McGinley Dynamic moving average (12)
        /// </summary>
        MGD
    }
}
