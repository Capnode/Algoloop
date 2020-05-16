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

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Algoloop.Model
{
    [DataContract]
    public class StrategiesModel
    {
        public const int version = 1;

        [Browsable(false)]
        [DataMember]
        public int Version { get; set; }

        [Browsable(false)]
        [DataMember]
        public Collection<StrategyModel> Strategies { get; } = new Collection<StrategyModel>();

        internal void Copy(StrategiesModel strategiesModel)
        {
            Strategies.Clear();
            foreach (StrategyModel strategy in strategiesModel.Strategies)
            {
                Strategies.Add(strategy);
            }
        }
    }
}
