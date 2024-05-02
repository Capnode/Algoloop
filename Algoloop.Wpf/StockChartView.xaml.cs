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

using Algoloop.Wpf.ViewModels;
using Algoloop.Wpf.ViewModels.Internal.Lean;
using Algoloop.Wpf.Internal;
using Ecng.Collections;
using Ecng.Serialization;
using QuantConnect;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Charting;
using StockSharp.Configuration;
using StockSharp.Xaml.Charting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Algoloop.Wpf
{
    /// <summary>
    /// 
    /// StockShart doc: https://doc.stocksharp.com/topics/StockSharpAbout.html
    /// </summary>
    public partial class StockChartView : UserControl
    {
        private const int MaxElementsInArea = 10;
        private const int MinBars = int.MaxValue;

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(ObservableCollection<IChartViewModel>),
            typeof(StockChartView),
            new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));
        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
            "Settings",
            typeof(string),
            typeof(StockChartView),
            new FrameworkPropertyMetadata { BindsTwoWayByDefault = true, DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

        private bool _isLoaded = false;
        private bool _inRedraw = false;
        private readonly CachedSynchronizedDictionary<IChartIndicatorElement, IIndicator> _indicators = new();
        private readonly CollectionSecurityProvider _securityProvider = new();
        private readonly IDictionary<string, SymbolViewModel> _symbols = new Dictionary<string, SymbolViewModel>();

        public StockChartView()
        {
            InitializeComponent();
            Loaded += OnLoaded;

            _chart.Name = "Chart";
            _chart.IsAutoScroll = true;
            _chart.MinimumRange = MinBars;
            _chart.ShowOverview = true;
            _chart.ShowPerfStats = false;
            _chart.AllowAddCandles = true;
            _chart.AllowAddIndicators = true;
            _chart.AllowAddOrders = false;
            _chart.AllowAddOwnTrades = false;
            _chart.AllowAddAxis = true;
            _chart.AllowAddArea = true;
            _chart.AllowDrop = true;

            _chart.SubscribeCandleElement += OnSubscribeCandleElement;
            _chart.SubscribeIndicatorElement += OnSubscribeIndicatorElement;
            _chart.UnSubscribeElement += OnUnSubscribeElement;
            _chart.AnnotationCreated += OnAnnotationCreated;
            _chart.AnnotationModified += OnAnnotationModified;
            _chart.AnnotationDeleted += OnAnnotationDeleted;
            _chart.AnnotationSelected += OnAnnotationSelected;
            _chart.SettingsChanged += OnSettingsChanged;
            _chart.SubscribeOrderElement += OnSubscribeOrderElement;
            _chart.SubscribeTradeElement += OnSubscribeTradeElement;
            _chart.RegisterOrder += OnRegisterOrder;
            _chart.MoveOrder += OnMoveOrder;
            _chart.CancelOrder += OnCancelOrder;
            _chart.Drop += OnDrop;

            _chart.SecurityProvider = _securityProvider;
        }

        public ObservableCollection<IChartViewModel> ItemsSource
        {
            get => (ObservableCollection<IChartViewModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string Settings
        {
            get => (string)GetValue(SettingsProperty);
            set => SetValue(SettingsProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StockChartView chart = d as StockChartView;
            if (e.OldValue != null)
            {
                // Unsubscribe from CollectionChanged on the old collection
                var coll = e.OldValue as INotifyCollectionChanged;
                coll.CollectionChanged -= chart.OnCollectionChanged;
            }

            if (e.NewValue != null)
            {
                // Subscribe to CollectionChanged on the new collection
                var coll = e.NewValue as ObservableCollection<IChartViewModel>;
                coll.CollectionChanged += chart.OnCollectionChanged;
            }

            var charts = e.NewValue as IEnumerable<IChartViewModel>;
            chart.ItemsSourceChanged(charts);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is not Collection<IChartViewModel> collection) return;
            List<IChartViewModel> charts = collection.ToList();
            ItemsSourceChanged(charts);
        }

        private void ItemsSourceChanged(IEnumerable<IChartViewModel> charts)
        {
            _combobox.Items.Clear();
            if (charts == null) return;
            foreach (IChartViewModel chart in charts)
            {
                _combobox.Items.Add(new ChartItemViewModel(chart));
            }

            _combobox.SelectedIndex = 0;
            _combobox.Visibility = _combobox.Items.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            if (_isLoaded)
            {
                SetVisibleCharts();
                RedrawCharts();
            }
        }

        private void Combobox_DropDownClosed(object sender, EventArgs e)
        {
            RedrawCharts();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded) return;
            Window window = Window.GetWindow(this);
            window.Closing += OnUnloaded;

            try
            {
                _chart.FillIndicators();
                byte[] bytes = Encoding.Default.GetBytes(Settings);
                SettingsStorage storage = bytes.Deserialize<SettingsStorage>();
                _chart.LoadIfNotNull(storage);
                SetVisibleCharts();
                _isLoaded = true;
            }
            catch (Exception)
            {
            }

            RedrawCharts();
        }

        private void SetVisibleCharts()
        {
            bool visible = _chart.Areas.Count == 0; // Make first visible if no charts
            foreach (ChartItemViewModel item in _combobox.Items)
            {
                visible |= item.Chart.IsVisible;
                for (int i = 0; !visible && i < _chart.Areas.Count; i++)
                {
                    string title = _chart.Areas[i].Title;
                    if (item.Chart.Title.Equals(title))
                    {
                        visible = true;
                    }
                }

                item.IsVisible = visible;
                visible = false;
            }
        }

        private void OnUnloaded(object sender, CancelEventArgs e)
        {
            var settingsStorage = new SettingsStorage();
            _chart.Save(settingsStorage);
            byte[] bytes = settingsStorage.Serialize();
            Settings = Encoding.Default.GetString(bytes);
        }

        private void OnSettingsChanged()
        {
            //Debug.WriteLine($"OnSettingsChanged()");
        }

        private void OnAnnotationSelected(IChartAnnotation arg1, ChartDrawData.AnnotationData arg2)
        {
            //Debug.WriteLine($"OnAnnotationSelected()");
        }

        private void OnAnnotationDeleted(IChartAnnotation obj)
        {
            //Debug.WriteLine($"OnAnnotationDeleted()");
        }

        private void OnAnnotationModified(IChartAnnotation arg1, ChartDrawData.AnnotationData arg2)
        {
            //Debug.WriteLine($"OnAnnotationModified()");
        }

        private void OnAnnotationCreated(IChartAnnotation obj)
        {
            //Debug.WriteLine($"OnAnnotationCreated()");
        }

        private void OnRegisterOrder(IChartArea arg1, StockSharp.BusinessEntities.Order arg2)
        {
            //Debug.WriteLine($"OnRegisterOrder()");
        }

        private void OnMoveOrder(StockSharp.BusinessEntities.Order arg1, decimal arg2)
        {
            //Debug.WriteLine($"OnMoveOrder()");
        }

        private void OnCancelOrder(StockSharp.BusinessEntities.Order obj)
        {
            //Debug.WriteLine($"OnCancelOrder()");
        }

        private void OnSubscribeTradeElement(IChartTradeElement arg1, StockSharp.BusinessEntities.Security arg2)
        {
            //Debug.WriteLine($"OnSubscribeTradeElement()");
        }

        private void OnSubscribeOrderElement(IChartOrderElement arg1, StockSharp.BusinessEntities.Security arg2)
        {
            //Debug.WriteLine($"OnSubscribeOrderElement()");
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (sender is not ChartPanel panel) return;
            if (e.Data.GetData(typeof(SymbolViewModel)) is not SymbolViewModel reference) return;
            foreach (ChartItemViewModel item in _combobox.Items)
            {
                if (item.Chart is not SymbolChartViewModel target) break;
                target.Symbol.AddReferenceSymbol(reference);
                break;
            }

            e.Handled = true;
        }

        private void OnSubscribeCandleElement(IChartCandleElement element, CandleSeries candles)
        {
            if (_inRedraw) return;
            //Debug.WriteLine($"OnSubscribeCandleElement({element.FullTitle ?? "-"}, {candles.Security?.Id ?? "-"})");
            if (candles.Security == null) return;
            _securityProvider.Add(candles.Security);
            if (!_isLoaded) return;
            RedrawCharts();
        }

        private void OnSubscribeIndicatorElement(IChartIndicatorElement element, CandleSeries candles, IIndicator indicator)
        {
            if (_inRedraw) return;
            //Debug.WriteLine($"OnSubscribeIndicatorElement({element.FullTitle ?? "-"}, {candles.Security?.Id ?? "-"}, {indicator.Name ?? "-"})");
            _indicators[element] = indicator;
            if (!_isLoaded) return;
            RedrawCharts();
        }

        private void OnUnSubscribeElement(IChartElement element)
        {
            if (_inRedraw) return;
            //Debug.WriteLine($"OnUnSubscribeElement({element.FullTitle ?? "-"})");
            if (element is IChartIndicatorElement indElem)
            {
                _indicators.Remove(indElem);
            }
        }

        private void RedrawCharts()
        {
            _inRedraw = true;
            _chart.IsAutoRange = true;
            _chart.Reset(_chart.Elements);

            // Collect time-value points of all Equity curves
            Dictionary<IChartLineElement, decimal> curves = new();
            Dictionary<DateTimeOffset, List<Tuple<IChartLineElement, decimal>>> points = new();
            int areaId = 0;
            foreach (ChartItemViewModel item in _combobox.Items)
            {
                if (!item.IsVisible) continue;
                IChartArea area;
                if (areaId < _chart.Areas.Count)
                {
                    area = _chart.Areas[areaId];
                    while (HasIOnlyIndicatorElement(area))
                    {
                        if (++areaId < _chart.Areas.Count)
                        {
                            area = _chart.Areas[areaId];
                        }
                        else
                        {
                            area = new ChartArea();
                            _chart.AddArea(area);
                        }
                    }
                }
                else
                {
                    area = new ChartArea();
                    _chart.AddArea(area);
                }

                areaId++;
                area.Title = item.Chart.Title;
                if (item.Chart is not ChartViewModel chart) continue;

                int seriesAreaId = 0;
                int elementId = 0;
                foreach (BaseSeries series in chart.Chart.Series.Values)
                {
                    if (elementId >= MaxElementsInArea)
                    {
                        // Create new area for extra series
                        seriesAreaId += 1;
                        elementId = 0;
                        if (_chart.Areas.Count <= areaId)
                        {
                            _chart.AddArea(new ChartArea());
                        }

                        area = _chart.Areas[areaId++];
                        area.Title = item.Chart.Title + seriesAreaId.ToString();
                    }

                    IChartLineElement element;
                    if (area.Elements.Count <= elementId)
                    {
                        element = _chart.CreateLineElement();
                        element.IsVisible = elementId == 0;
                        _chart.AddElement(area, element);
                    }

                    element = area.Elements[elementId] as ChartLineElement;
                    element.Update(series);
                    elementId++;
                    element.YAxisId = area.YAxises.First().Id;
                    foreach (EquityData equityData in series.ToEquityData())
                    {
                        decimal value = equityData.Value;
                        if (!curves.ContainsKey(element))
                        {
                            curves.Add(element, value);
                        }

                        DateTimeOffset time = equityData.Time.Date;
                        if (!points.TryGetValue(time, out List<Tuple<IChartLineElement, decimal>> list))
                        {
                            list = new List<Tuple<IChartLineElement, decimal>>();
                            points.Add(time, list);
                        }

                        list.Add(new Tuple<IChartLineElement, decimal>(element, value));
                    }
                }

                // Remove unused elements
                var unusedElements = new List<IChartElement>();
                while (elementId < area.Elements.Count)
                {
                    IChartElement element = area.Elements[elementId++];
                    if (element is IChartIndicatorElement) continue; // Keep indicators
                    unusedElements.Add(element);
                }

                unusedElements.ForEach(m => _chart.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     RemoveElement(area, m));
            }

            // Remove unused areas if not containing indicators
            var unusedAreas = new List<IChartArea>();
            while (areaId < _chart.Areas.Count)
            {
                IChartArea area = _chart.Areas[areaId++];
                if (!HasIOnlyIndicatorElement(area))
                {
                    unusedAreas.Add(area);
                }
            }
            unusedAreas.ForEach(m => _chart.RemoveArea(m));

            // Draw all equity curves in time order, moment by moment
            foreach (KeyValuePair<DateTimeOffset, List<Tuple<IChartLineElement, decimal>>> moment in points.OrderBy(m => m.Key))
            {
                DateTimeOffset time = moment.Key;
                ChartDrawData chartData = new();
                IChartDrawData.IChartDrawDataItem chartGroup = chartData.Group(time);
                foreach (KeyValuePair<IChartLineElement, decimal> curve in curves)
                {
                    IChartLineElement lineElement = curve.Key;
                    decimal value = curve.Value;

                    // Use actual point it available
                    Tuple<IChartLineElement, decimal> pair = moment.Value.Find(m => m.Item1.Equals(curve.Key));
                    if (pair != default)
                    {
                        value = pair.Item2;
                        curves[lineElement] = value;
                    }
                    chartGroup.Add(lineElement, value);
                }

                _chart.Draw(chartData);
            }

            // Draw SymbolSeries
            areaId = 0;
            bool doIndicators = true;
            foreach (ChartItemViewModel item in _combobox.Items)
            {
                if (areaId >= _chart.Areas.Count) break;
                if (!item.IsVisible) continue;
                if (item.Chart is not SymbolChartViewModel chart) continue;
                IChartArea area = _chart.Areas[areaId++];
                while (HasIOnlyIndicatorElement(area))
                {
                    Debug.Assert(areaId < _chart.Areas.Count, "No area found");
                    area = _chart.Areas[areaId++];
                }

                RedrawChart(area, chart.Symbol, doIndicators);
                doIndicators = false;
            }

            _chart.IsAutoRange = false; // Allow user to adjust range
            _inRedraw = false;
        }

        private static bool HasIOnlyIndicatorElement(IChartArea area)
        {
            bool indicator = false;
            foreach (IChartElement element in area.Elements)
            {
                if (element is not IChartIndicatorElement) return false;
                indicator = true;
            }

            return indicator;
        }

        private void RedrawChart(IChartArea area, SymbolViewModel symbol, bool doIndicators)
        {
            StockSharp.BusinessEntities.Security security = symbol.Model.ToSecurity();
            _symbols[security.Id] = symbol;
            _securityProvider.Add(security);
            IChartCandleElement element;
            if (area.Elements.Count < 1)
            {
                element = _chart.CreateCandleElement();
                element.Update(symbol.Model);
                element.YAxisId = area.YAxises.First().Id;
                CandleSeries series = new CandleSeries(
                    typeof(TimeFrameCandle),
                    security,
                    symbol.Market.ChartResolution.ToTimeSpan());
                _chart.AddElement(area, element, series);
            }
            else
            {
                element = area.Elements[0] as IChartCandleElement;
                element.Update(symbol.Model);
            }

            // Process Candles
            foreach ((IChartIndicatorElement indicatorElement, IIndicator indicator) in _indicators.CachedPairs)
            {
                indicator.Reset();
            }

            var chartData = new ChartDrawData();
            IEnumerable<Candle> candles = symbol.History()?.ToCandles() ?? Enumerable.Empty<Candle>();
            foreach (Candle candle in candles)
            {
                IChartDrawData.IChartDrawDataItem chartGroup = chartData.Group(candle.OpenTime);
                chartGroup.Add(element, candle);
                if (!doIndicators) continue;
                foreach ((IChartIndicatorElement indicatorElement, IIndicator indicator) in _indicators.CachedPairs)
                {
                    chartGroup.Add(indicatorElement, indicator.Process(candle));

                    // Adjust axes
                    IChartArea indicatorArea = indicatorElement.ChartArea;
                    indicatorElement.YAxisId = indicatorArea.YAxises.First().Id;
                    indicatorArea.YAxises.RemoveRange(indicatorArea.YAxises.Skip(1));
                    indicatorArea.Title = indicatorElement.FullTitle;
                }
            }

            _chart.Draw(chartData);
        }
    }
}
