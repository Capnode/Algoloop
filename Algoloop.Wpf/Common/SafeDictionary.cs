using System;
/*
 * Copyright 2019 Capnode AB
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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Algoloop.Wpf.Common
{
    [Serializable]
    public class SafeDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public SafeDictionary()
            : base()
        {
        }

        public SafeDictionary(IDictionary<TKey, TValue> dict)
            : base(dict)
        {
        }

        protected SafeDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public new TValue this[TKey key]
        {
            get
            {
                if (!ContainsKey(key))
                    return default;
                else
                    return base[key];
            }
            set
            {
                if (!ContainsKey(key))
                    Add(key, value);
                else
                    base[key] = value;
            }
        }
    }
}
