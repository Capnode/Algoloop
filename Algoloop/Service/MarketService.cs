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

using Algoloop.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Algoloop.Service
{
    [DataContract]
    public class MarketService
    {
        [Browsable(false)]
        [DataMember]
        public List<MarketModel> Markets { get; } = new List<MarketModel>();

        internal void Copy(MarketService marketsModel)
        {
            Markets.Clear();
            Markets.AddRange(marketsModel.Markets);
        }

        internal IReadOnlyList<MarketModel> GetMarkets()
        {
            return Markets;
        }

        internal MarketModel GetMarket(string provider)
        {
            return Markets.Find(m => m.Name.Equals(provider));
        }
    }
}
