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

using System;
using System.Collections.Generic;

namespace Algoloop.Charts
{
    public class ChartViewModel
    {
        public ChartViewModel(
            string name,
            string graphType,
            System.Drawing.Color color,
            IEnumerable<object> series,
            string datePath,
            DateTime startDate,
            DateTime endDate,
            string valuePath,
            string openPath,
            string highPath,
            string lowPath,
            string closePath)
        {
            if (series == null) throw new ArgumentNullException(nameof(series));
            Color = ToMediaBrush(color);
            Title = name;
            //DataSet = new DataSet()
            //{
            //    ID = "id",
            //    Title = name,
            //    ShortTitle = name,
            //    DateMemberPath = datePath,
            //    OpenMemberPath = openPath,
            //    HighMemberPath = highPath,
            //    LowMemberPath = lowPath,
            //    CloseMemberPath = closePath,
            //    ValueMemberPath = valuePath,
            //    IsSelectedForComparison = false,
            //    ItemsSource = series,
            //    IsVisibleInCompareDataSetSelector = false,
            //    IsVisibleInMainDataSetSelector = false,
            //    StartDate = startDate,
            //    EndDate = endDate
            //};
        }

        public System.Windows.Media.Brush Color { get; }

        public string Title { get; }

        private static System.Windows.Media.Brush ToMediaBrush(System.Drawing.Color color)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, color.R, color.G, color.B));
        }
    }
}
