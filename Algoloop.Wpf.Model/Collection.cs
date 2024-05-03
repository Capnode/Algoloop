/*
 * Copyright 2021 Capnode AB
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

namespace Algoloop.Wpf.Model
{
    public static class Collection
    {
        public static bool SmartCopy<T>(IEnumerable<T> src, ICollection<T> dest)
        {
            if (Equals(src, dest)) return false;

            dest.Clear();
            foreach (T item in src)
            {
                dest.Add(item);
            }
            return true;
        }

        public static bool Equals<T>(IEnumerable<T> src, IEnumerable<T> dest)
        {
            if (src == default && dest == default) return true;
            if (src == default || dest == default) return false;
            IEnumerator<T> iSrc = src.GetEnumerator();
            IEnumerator<T> iDest = dest.GetEnumerator();

            // Return if collections are equal
            while (true)
            {
                bool bSrc = iSrc.MoveNext();
                bool bDest = iDest.MoveNext();
                if (bSrc != bDest) return false;
                if (!bSrc) return true;
                if (!iSrc.Current.Equals(iDest.Current)) return false;
            }
        }

        public static int GetHashCode<T>(IEnumerable<T> src)
        {
            int hash = 0;
            foreach (T item in src)
            {
                hash ^= item.GetHashCode();
            }

            return hash;                
        }
    }
}
