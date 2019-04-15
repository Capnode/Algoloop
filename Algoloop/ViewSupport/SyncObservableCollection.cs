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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace Algoloop.ViewSupport
{
    /// <summary>
    /// SyncObservableCollection class
    /// </summary>
    public class SyncObservableCollection<T> : ObservableCollection<T>
    {
        private object _lock = new object();

        public SyncObservableCollection()
        {
            BindingOperations.EnableCollectionSynchronization(this, _lock);
        }

        public SyncObservableCollection(IEnumerable<T> list)
            : base(list)
        {
            BindingOperations.EnableCollectionSynchronization(this, _lock);
        }

        public void Sort()
        {
            if (this.Count <= 1)
                return;

            List<T> sorted = this.OrderBy(x => x).ToList();
            for (int i = 0; i < sorted.Count(); i++)
                this.Move(this.IndexOf(sorted[i]), i);
        }
    }
}
