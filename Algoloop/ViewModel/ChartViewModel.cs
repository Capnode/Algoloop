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
using LiveCharts;
using QuantConnect;
using System;

public class ChartViewModel : ViewModelBase
{
    private double _timeFrom;
    private double _timeTo;

    public ChartViewModel(string title, LiveCharts.Wpf.Series chart, LiveCharts.Wpf.Series scroll, DateTime first, DateTime last, Resolution resolution)
    {
//        Debug.WriteLine($"{title} chart={chart.Values.Count} scroll={scroll.Values.Count} {first} {last} {unit}");

        Title = title;
        Chart.Add(chart);
        Scroll.Add(scroll);
        TimeFrom = first.Ticks;
        TimeTo = last.Ticks;

        string format = "G";
        switch (resolution)
        {
            case Resolution.Daily:
                Unit = TimeSpan.FromDays(1).Ticks;
                format = "d";
                break;
            case Resolution.Hour:
                Unit = TimeSpan.FromHours(1).Ticks;
                format = "g";
                break;
            case Resolution.Minute:
                Unit = TimeSpan.FromMinutes(1).Ticks;
                format = "G";
                break;
            case Resolution.Second:
                Unit = TimeSpan.FromSeconds(1).Ticks;
                format = "G";
                break;
            case Resolution.Tick:
                Unit = TimeSpan.FromTicks(1).Ticks;
                format = "G";
                break;
        }

        Formatter = value =>
        {
            return new DateTime((long)value).ToString(format);
        };

    }

    public string Title { get; }
    public SeriesCollection Chart { get; } = new SeriesCollection();
    public SeriesCollection Scroll { get; } = new SeriesCollection();
    public double Unit { get; }
    public Func<double, string> Formatter { get; set; }
    public double TimeFrom
    {
        get => _timeFrom;
        set
        {
            Set(ref _timeFrom, value);
        }
    }

    public double TimeTo
    {
        get => _timeTo;
        set
        {
            Set(ref _timeTo, value);
        }
    }
}
