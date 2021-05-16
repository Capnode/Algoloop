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

using Ecng.Backup;
using Ecng.Backup.Yandex;
using Ecng.Collections;
using Ecng.Common;
using Ecng.Configuration;
using Ecng.Xaml;
using Ecng.Xaml.Yandex;
using MoreLinq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using StockSharp.Messages;
using StockSharp.Xaml.Charting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Algoloop.Charts
{
    public partial class StockChart : UserControl, ICandleBuilderSubscription
    {
        public static readonly DependencyProperty ItemsSourceProperty = 
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<ChartViewModel>),
            typeof(StockChart), new PropertyMetadata(null, new PropertyChangedCallback(OnItemsSourceChanged)));

        private readonly CachedSynchronizedOrderedDictionary<DateTimeOffset, Candle> _allCandles =
            new CachedSynchronizedOrderedDictionary<DateTimeOffset, Candle>();
        private readonly SynchronizedList<CandleMessage> _updatedCandles = new SynchronizedList<CandleMessage>();
        private readonly SynchronizedList<Action> _dataThreadActions = new SynchronizedList<Action>();
        private readonly CachedSynchronizedDictionary<ChartIndicatorElement, IIndicator> _indicators =
            new CachedSynchronizedDictionary<ChartIndicatorElement, IIndicator>();
        private static readonly TimeSpan _realtimeInterval = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan _drawInterval = TimeSpan.FromMilliseconds(100);
        private readonly CandlesHolder _holder = new CandlesHolder();
        private readonly CollectionSecurityProvider _securityProvider = new CollectionSecurityProvider();
        private readonly List<ChartModel> _models = new List<ChartModel>();
        private readonly TestMarketSubscriptionProvider _testProvider = new TestMarketSubscriptionProvider();
        private readonly IdGenerator _transactionIdGenerator = new IncrementalIdGenerator();
        private readonly ICandleBuilderValueTransform _candleTransform = new TickCandleBuilderValueTransform();
        private readonly CandleBuilderProvider _builderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
        private ICandleBuilder _candleBuilder;
        private readonly Timer _dataTimer;
        private bool _historyLoaded;
        private ChartAnnotation _annotation;
        private ChartDrawData.AnnotationData _annotationData;
        private readonly SyncObject _timerLock = new SyncObject();
        private bool _isInTimerHandler;
        private DateTime _lastRealtimeUpdateTime;
        private DateTime _lastDrawTime;
        private bool _isRealTime;
        private DateTimeOffset _lastTime;
        private long _transactionId;
        private DateTimeOffset _lastCandleDrawTime;
        private Color _candleDrawColor;
        private ChartCandleElement _candleElement;
        private bool _drawWithColor;
        private ChartArea _areaComb;
        private Security _security;
        private RandomWalkTradeGenerator _tradeGenerator;
        private string _historyPath;
        private MarketDataMessage _mdMsg;

        public StockChart()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            _dataTimer = ThreadingHelper
                .Timer(OnDataTimer)
                .Interval(TimeSpan.FromMilliseconds(1));
            ConfigManager.RegisterService<ISubscriptionProvider>(_testProvider);
            ConfigManager.RegisterService<ISecurityProvider>(_securityProvider);
        }

        MarketDataMessage ICandleBuilderSubscription.Message => _mdMsg;
        VolumeProfileBuilder ICandleBuilderSubscription.VolumeProfile { get; set; }

        public CandleMessage CurrentCandle { get; set; }

        public ObservableCollection<ChartViewModel> ItemsSource
        {
            get => (ObservableCollection<ChartViewModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StockChart chart = d as StockChart;
            Debug.Assert(chart != null);

            if (e.OldValue != null)
            {
                // Unsubscribe from CollectionChanged on the old collection
                var coll = e.OldValue as INotifyCollectionChanged;
                coll.CollectionChanged -= chart.OnCollectionChanged;
            }

            if (e.NewValue != null)
            {
                // Subscribe to CollectionChanged on the new collection
                var coll = e.NewValue as ObservableCollection<ChartViewModel>;
                coll.CollectionChanged += chart.OnCollectionChanged;
            }

            var charts = e.NewValue as IEnumerable<ChartViewModel>;
            chart.OnItemsSourceChanged(charts);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            List<ChartViewModel> charts = e.NewItems?.Cast<ChartViewModel>().ToList();
            OnItemsSourceChanged(charts);
        }

        private void OnItemsSourceChanged(IEnumerable<ChartViewModel> charts)
        {
            // Clear charts
            _combobox.Items.Clear();
            _models.Clear();
            if (charts == null)
                return;

            bool selected = true;
            foreach (ChartViewModel chart in charts)
            {
                var model = new ChartModel(chart, selected || IsDefaultSelected(chart.Title));
                _combobox.Items.Add(model);
                selected = false;
            }

            _combobox.SelectedIndex = 0;
            _combobox.Visibility = _combobox.Items.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

            RedrawCharts();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Chart.FillIndicators();
            Chart.SubscribeCandleElement += Chart_OnSubscribeCandleElement;
            Chart.SubscribeIndicatorElement += Chart_OnSubscribeIndicatorElement;
            Chart.UnSubscribeElement += Chart_OnUnSubscribeElement;
            Chart.AnnotationCreated += ChartOnAnnotationCreated;
            Chart.AnnotationModified += ChartOnAnnotationModified;
            Chart.AnnotationDeleted += ChartOnAnnotationDeleted;
            Chart.AnnotationSelected += ChartOnAnnotationSelected;
            Chart.RegisterOrder += (area, order) =>
            {
                MessageBox.Show($"RegisterOrder: sec={order.Security.Id}, {order.Direction} {order.Volume}@{order.Price}");
            };

            Window window = Application.Current.MainWindow;
            ConfigManager.RegisterService<IBackupService>(new YandexDiskService(YandexLoginWindow.Authorize(window)));
            _historyPath = @"C:\Source\StockSharp\packages\stocksharp.samples.historydata\1.0.0\HistoryData";
            Chart.SecurityProvider = _securityProvider;
            WpfSupport.UiThread(() => RefreshCharts());
        }

        private void Chart_OnSubscribeCandleElement(ChartCandleElement el, CandleSeries ser)
        {
            CurrentCandle = null;
            _historyLoaded = false;
            _allCandles.Clear();
            _updatedCandles.Clear();
            _dataThreadActions.Clear();

            Chart.Reset(new[] { el });

            LoadData(ser);
        }

        private void Chart_OnSubscribeIndicatorElement(ChartIndicatorElement element, CandleSeries series, IIndicator indicator)
        {
            _dataThreadActions.Add(() =>
            {
                var oldReset = Chart.DisableIndicatorReset;
                try
                {
                    Chart.DisableIndicatorReset = true;
                    indicator.Reset();
                }
                finally
                {
                    Chart.DisableIndicatorReset = oldReset;
                }

                var chartData = new ChartDrawData();

                foreach (var candle in _allCandles.CachedValues)
                    chartData.Group(candle.OpenTime).Add(element, indicator.Process(candle));

                Chart.Reset(new[] { element });
                Chart.Draw(chartData);

                _indicators[element] = indicator;
            });
        }

        private void Chart_OnUnSubscribeElement(IChartElement element)
        {
            _dataThreadActions.Add(() =>
            {
                if (element is ChartIndicatorElement indElem)
                    _indicators.Remove(indElem);
            });
        }

        private void ChartOnAnnotationCreated(ChartAnnotation ann) => _annotation = ann;

        private void ChartOnAnnotationModified(ChartAnnotation ann, ChartDrawData.AnnotationData data)
        {
            _annotation = ann;
            _annotationData = data;
        }

        private void ChartOnAnnotationDeleted(ChartAnnotation ann)
        {
            if (_annotation == ann)
            {
                _annotation = null;
                _annotationData = null;
            }
        }

        private void ChartOnAnnotationSelected(ChartAnnotation ann, ChartDrawData.AnnotationData data)
        {
            _annotation = ann;
            _annotationData = data;
        }

        private void RefreshCharts()
        {
            Chart.ClearAreas();

            _areaComb = new ChartArea();

            var yAxis = _areaComb.YAxises.First();

            yAxis.AutoRange = true;
            Chart.IsAutoRange = true;
            Chart.IsAutoScroll = true;

            Chart.AddArea(_areaComb);

            SecurityId[] secs = LocalMarketDataDrive.GetAvailableSecurities(_historyPath).ToArray();
            SecurityId id = secs.Single();

            _security = new Security
            {
                Id = id.ToStringId(),
                Code = id.SecurityCode,
                Type = SecurityTypes.Future,
                PriceStep = id.SecurityCode.StartsWithIgnoreCase("RI") ? 10 :
                    id.SecurityCode.Contains("ES") ? 0.25m :
                    0.01m,
                Board = ExchangeBoard.Associated
            };

            _securityProvider.Clear();
            _securityProvider.Add(_security);

            _tradeGenerator = new RandomWalkTradeGenerator(id);
            _tradeGenerator.Init();
            _tradeGenerator.Process(_security.ToMessage());

            var series = new CandleSeries(typeof(TimeFrameCandle), _security, TimeSpan.FromMinutes(1))
            { IsCalcVolumeProfile = true };

            _candleElement = new ChartCandleElement();
            Chart.AddElement(_areaComb, _candleElement, series);
        }

        private void LoadData(CandleSeries series)
        {
            var msgType = series.CandleType.ToCandleMessageType();

            _transactionId = _transactionIdGenerator.GetNextId();
            _holder.Clear();
            _holder.CreateCandleSeries(_transactionId, series);

            _candleTransform.Process(new ResetMessage());
            _candleBuilder = _builderProvider.Get(msgType);

            var storage = new StorageRegistry();

            //BusyIndicator.IsBusy = true;

            var path = _historyPath;
            bool isBuild = true;
            StorageFormats format = StorageFormats.Binary;

            var maxDays = (isBuild || series.CandleType != typeof(TimeFrameCandle))
                ? 2
                : 30 * (int)((TimeSpan)series.Arg).TotalMinutes;

            _mdMsg = series.ToMarketDataMessage(true);

            Task.Factory.StartNew(() =>
            {
                var date = DateTime.MinValue;

                if (isBuild)
                {
                    foreach (var tick in storage.GetTickMessageStorage(series.Security.ToSecurityId(), new LocalMarketDataDrive(path), format).Load())
                    {
                        _tradeGenerator.Process(tick);

                        if (_candleTransform.Process(tick))
                        {
                            var candles = _candleBuilder.Process(this, _candleTransform);

                            foreach (var candle in candles)
                            {
                                _updatedCandles.Add(candle.TypedClone());
                            }
                        }

                        _lastTime = tick.ServerTime;

                        if (date != tick.ServerTime.Date)
                        {
                            date = tick.ServerTime.Date;

                            //var str = date.To<string>();
                            //this.GuiAsync(() => BusyIndicator.BusyContent = str);

                            maxDays--;

                            if (maxDays == 0)
                                break;
                        }
                    }
                }
                else
                {
                    foreach (var candleMsg in storage.GetCandleMessageStorage(msgType, series.Security.ToSecurityId(), series.Arg, new LocalMarketDataDrive(path), format).Load())
                    {
                        if (candleMsg.State != CandleStates.Finished)
                            candleMsg.State = CandleStates.Finished;

                        CurrentCandle = candleMsg;
                        _updatedCandles.Add(candleMsg);

                        _lastTime = candleMsg.OpenTime;

                        if (candleMsg is TimeFrameCandleMessage)
                            _lastTime += (TimeSpan)series.Arg;

                        _tradeGenerator.Process(new ExecutionMessage
                        {
                            ExecutionType = ExecutionTypes.Tick,
                            SecurityId = series.Security.ToSecurityId(),
                            ServerTime = _lastTime,
                            TradePrice = candleMsg.ClosePrice,
                        });

                        if (date != candleMsg.OpenTime.Date)
                        {
                            date = candleMsg.OpenTime.Date;

                            //var str = date.To<string>();
                            //this.GuiAsync(() => BusyIndicator.BusyContent = str);

                            maxDays--;

                            if (maxDays == 0)
                                break;
                        }
                    }
                }

                _historyLoaded = true;
            })
            .ContinueWith(t =>
            {
                if (t.Exception != null)
                    Error(t.Exception.Message);

                //BusyIndicator.IsBusy = false;
                Chart.IsAutoRange = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
//            _dataTimer.Dispose();
        }

        private void OnDataTimer()
        {
            lock (_timerLock)
            {
                if (_isInTimerHandler)
                    return;

                _isInTimerHandler = true;
            }

            try
            {
                if (_dataThreadActions.Count > 0)
                {
                    Action[] actions = null;
                    _dataThreadActions.SyncDo(l => actions = l.CopyAndClear());
                    actions.ForEach(a => a());
                }

                var now = DateTime.UtcNow;
                DoIfTime(UpdateRealtimeCandles, now, ref _lastRealtimeUpdateTime, _realtimeInterval);
                DoIfTime(DrawChartElements, now, ref _lastDrawTime, _drawInterval);
            }
            catch (Exception ex)
            {
                ex.LogError();
            }
            finally
            {
                _isInTimerHandler = false;
            }
        }

        private static void DoIfTime(Action action, DateTime now, ref DateTime lastExecutTime, TimeSpan period)
        {
            if (now - lastExecutTime < period)
                return;

            lastExecutTime = now;
            action();
        }

        private static bool IsDefaultSelected(string title)
        {
            return title switch
            {
                "Net profit" => true,
                "Equity" => true,
                _ => false,
            };
        }

        private void Error(string msg)
        {
            new MessageBoxBuilder()
                .Owner(this)
                .Error()
                .Text(msg)
                .Show();
        }

        private static Color GetRandomColor() => Color.FromRgb((byte)RandomGen.GetInt(0, 255), (byte)RandomGen.GetInt(0, 255), (byte)RandomGen.GetInt(0, 255));

        private void UpdateRealtimeCandles()
        {
            if (!_historyLoaded || !_isRealTime)
                return;

            _lastTime += TimeSpan.FromMilliseconds(RandomGen.GetInt(100, 20000));
        }

        private void DrawChartElements()
        {
            var messages = _updatedCandles.SyncGet(uc => uc.CopyAndClear());

            if (messages.Length == 0)
                return;

            var lastTime = DateTimeOffset.MinValue;
            var candlesToUpdate = new List<Candle>();

            foreach (var message in messages.Reverse())
            {
                if (lastTime == message.OpenTime)
                    continue;

                lastTime = message.OpenTime;

                var info = _holder.UpdateCandles(_transactionId, message);

                if (info == null)
                    continue;

                var candle = info.Item2;

                if (candlesToUpdate.Count == 0 || candlesToUpdate.Last() != candle)
                    candlesToUpdate.Add(candle);
            }

            candlesToUpdate.Reverse();

            foreach (var candle in candlesToUpdate)
                _allCandles[candle.OpenTime] = candle;

            ChartDrawData chartData = null;

            foreach (var candle in candlesToUpdate)
            {
                if (chartData == null)
                    chartData = new ChartDrawData();

                if (_lastCandleDrawTime != candle.OpenTime)
                {
                    _lastCandleDrawTime = candle.OpenTime;
                    _candleDrawColor = GetRandomColor();
                }

                var chartGroup = chartData.Group(candle.OpenTime);
                chartGroup.Add(_candleElement, candle);
                chartGroup.Add(_candleElement, _drawWithColor ? _candleDrawColor : (Color?)null);

                foreach (var pair in _indicators.CachedPairs)
                {
                    chartGroup.Add(pair.Key, pair.Value.Process(candle));
                }
            }

            if (chartData != null)
                Chart.Draw(chartData);
        }

        private void RedrawCharts()
        {
            foreach (var item in _combobox.Items)
            {
                if (item is ChartModel model && model.IsSelected)
                {
                    RedrawChart(model);
                }
            }
        }

        private void RedrawChart(ChartModel model)
        {
        }

        private void Combobox_DropDownClosed(object sender, EventArgs e)
        {
            RedrawCharts();
        }

        private class TestMarketSubscriptionProvider : ISubscriptionProvider
        {
            private readonly HashSet<Subscription> _l1Subscriptions = new HashSet<Subscription>();

            public void UpdateData(Security sec, decimal price)
            {
                var ps = sec.PriceStep ?? 1;

                var msg = new Level1ChangeMessage
                {
                    SecurityId = sec.ToSecurityId(),
                    ServerTime = DateTimeOffset.Now,
                };

                if (RandomGen.GetBool())
                    msg.Changes.TryAdd(Level1Fields.BestBidPrice, price - RandomGen.GetInt(1, 10) * ps);

                if (RandomGen.GetBool())
                    msg.Changes.TryAdd(Level1Fields.BestAskPrice, price + RandomGen.GetInt(1, 10) * ps);

                foreach (var l1Subscriptions in _l1Subscriptions)
                {
                    _level1Received?.Invoke(l1Subscriptions, msg);
                }
            }

            private event Action<Subscription, Level1ChangeMessage> _level1Received;

            event Action<Subscription, Level1ChangeMessage> ISubscriptionProvider.Level1Received
            {
                add => _level1Received += value;
                remove => _level1Received -= value;
            }

            IEnumerable<Subscription> ISubscriptionProvider.Subscriptions => _l1Subscriptions;

            event Action<Subscription, Message> ISubscriptionProvider.SubscriptionReceived { add { } remove { } }

            event Action<Subscription, QuoteChangeMessage> ISubscriptionProvider.OrderBookReceived { add { } remove { } }

            event Action<Subscription, Trade> ISubscriptionProvider.TickTradeReceived { add { } remove { } }

            event Action<Subscription, Security> ISubscriptionProvider.SecurityReceived { add { } remove { } }

            event Action<Subscription, ExchangeBoard> ISubscriptionProvider.BoardReceived { add { } remove { } }

            event Action<Subscription, MarketDepth> ISubscriptionProvider.MarketDepthReceived { add { } remove { } }

            event Action<Subscription, OrderLogItem> ISubscriptionProvider.OrderLogItemReceived { add { } remove { } }

            event Action<Subscription, News> ISubscriptionProvider.NewsReceived { add { } remove { } }

            event Action<Subscription, Candle> ISubscriptionProvider.CandleReceived { add { } remove { } }

            event Action<Subscription, MyTrade> ISubscriptionProvider.OwnTradeReceived { add { } remove { } }

            event Action<Subscription, Order> ISubscriptionProvider.OrderReceived { add { } remove { } }

            event Action<Subscription, OrderFail> ISubscriptionProvider.OrderRegisterFailReceived { add { } remove { } }

            event Action<Subscription, OrderFail> ISubscriptionProvider.OrderCancelFailReceived { add { } remove { } }

            event Action<Subscription, OrderFail> ISubscriptionProvider.OrderEditFailReceived { add { } remove { } }

            event Action<Subscription, Portfolio> ISubscriptionProvider.PortfolioReceived { add { } remove { } }

            event Action<Subscription, Position> ISubscriptionProvider.PositionReceived { add { } remove { } }

            event Action<Subscription> ISubscriptionProvider.SubscriptionOnline { add { } remove { } }

            event Action<Subscription> ISubscriptionProvider.SubscriptionStarted { add { } remove { } }

            event Action<Subscription, Exception> ISubscriptionProvider.SubscriptionStopped { add { } remove { } }

            event Action<Subscription, Exception, bool> ISubscriptionProvider.SubscriptionFailed { add { } remove { } }

            void ISubscriptionProvider.Subscribe(Subscription subscription)
            {
                if (subscription.DataType == DataType.Level1)
                    _l1Subscriptions.Add(subscription);
            }

            void ISubscriptionProvider.UnSubscribe(Subscription subscription)
            {
                _l1Subscriptions.Remove(subscription);
            }
        }
    }
}
