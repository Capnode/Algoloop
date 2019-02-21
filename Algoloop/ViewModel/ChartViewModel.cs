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

using GalaSoft.MvvmLight;
using QuantConnect;
using QuantConnect.Data;
using System.Collections.Generic;

public class ChartViewModel : ViewModelBase
{
    public ChartViewModel(Series series)
    {
        Title = series.Name;
        Series = series;
        Data = null;
    }

    public ChartViewModel(Series series, IReadOnlyList<BaseData> data)
    {
        Title = series.Name;
        Series = series;
        Data = data;
    }

    public string Title { get; }
    public Series Series { get; }
    public IReadOnlyList<BaseData> Data { get; }
}
