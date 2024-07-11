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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Null data feed implementation. <seealso cref="DataManager"/>
    /// </summary>
    public class NullDataFeed : IDataFeed
    {
        /// <summary>
        /// Allows specifying if this implementation should throw always or not
        /// </summary>
        public bool ShouldThrow { get; set; } = true;

        /// <inheritdoc />
        public bool IsActive
        {
            get
            {
                if (!ShouldThrow)
                {
                    return true;
                }
                throw new NotImplementedException("Unexpected usage of null data feed implementation.");
            }
        }

        /// <inheritdoc />
        public void Initialize(
            IAlgorithm algorithm,
            AlgorithmNodePacket job,
            IResultHandler resultHandler,
            IMapFileProvider mapFileProvider,
            IFactorFileProvider factorFileProvider,
            IDataProvider dataProvider,
            IDataFeedSubscriptionManager subscriptionManager,
            IDataFeedTimeProvider dataFeedTimeProvider,
            IDataChannelProvider dataChannelProvider
            )
        {
            if (!ShouldThrow)
            {
                return;
            }
            throw new NotImplementedException("Unexpected usage of null data feed implementation.");
        }

        /// <inheritdoc />
        public Subscription CreateSubscription(SubscriptionRequest request)
        {
            if (!ShouldThrow)
            {
                return null;
            }
            throw new NotImplementedException("Unexpected usage of null data feed implementation.");
        }

        /// <inheritdoc />
        public void RemoveSubscription(Subscription subscription)
        {
            if (!ShouldThrow)
            {
                return;
            }
            throw new NotImplementedException("Unexpected usage of null data feed implementation.");
        }

        /// <inheritdoc />
        public void Exit()
        {
            if (!ShouldThrow)
            {
                return;
            }
            throw new NotImplementedException("Unexpected usage of null data feed implementation.");
        }
    }
}
