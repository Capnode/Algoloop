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

using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Algoloop.Wpf.View
{
    /// <summary>
    /// Interaction logic for DesktopView.xaml
    /// </summary>
    public partial class DesktopView : UserControl, IDisposable
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register("Port", typeof(string), typeof(DesktopView));

        private bool _isDisposed = false; // To detect redundant calls
        private readonly QueueLogHandler _logging;
        private Thread _thread;
        private AlgorithmNodePacket _job;
        private bool _liveMode;
        private volatile bool _stopServer = false;

        public DesktopView()
        {
            InitializeComponent();
            browser.Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            _logging = new QueueLogHandler();

//            Text = "QuantConnect Lean Algorithmic Trading Engine: v" + Globals.Version;
        }

        public string Port
        {
            get => (string)GetValue(PortProperty);
            set => SetValue(PortProperty, value);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WebBrowser wb)
            {
                // Silent
                FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField(
                    "_axIWebBrowser2",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (fiComWebBrowser == null) return;

                object objComWebBrowser = fiComWebBrowser.GetValue(wb);
                if (objComWebBrowser == null) return;

                objComWebBrowser.GetType().InvokeMember(
                    "Silent",
                    BindingFlags.SetProperty,
                    null,
                    objComWebBrowser,
                    new object[] { true },
                    CultureInfo.InvariantCulture);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == PortProperty)
            {
                string port = e.NewValue as string;
                if (string.IsNullOrEmpty(port))
                {
                    if (_thread != null)
                    {
                        _stopServer = true;
                        _thread.Join();
                        _thread = null;
                    }
                }
                else
                {
                    _stopServer = false;
                    Debug.Assert(_thread == null);
                    _thread = new Thread(() => Run(port));
                    _thread.SetApartmentState(ApartmentState.STA);
                    _thread.Start();
                }
            }
        }

        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_thread != null)
            {
                _stopServer = true;
                _thread.Join();
                _thread = null;
            }
        }

        /// <summary>
        /// This 0MQ Pull socket accepts certain messages from a 0MQ Push socket
        /// </summary>
        /// <param name="port">The port on which to listen</param>
        /// <param name="handler">The handler which will display the repsonses</param>
        private void Run(string port)
        {
            //Allow proper decoding of orders.
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new OrderJsonConverter() }
            };

            using var pullSocket = new PullSocket(">tcp://localhost:" + port);
            while (!_stopServer)
            {
                NetMQMessage message = null;
                if (!pullSocket.TryReceiveMultipartMessage(TimeSpan.FromSeconds(1), ref message))
                    continue;

                // There should only be 1 part messages
                if (message.FrameCount != 1) continue;

                var payload = message[0].ConvertToString();
                var packet = JsonConvert.DeserializeObject<Packet>(payload);

                switch (packet.Type)
                {
                    case PacketType.BacktestNode:
                        var backtestJobModel = JsonConvert.DeserializeObject<BacktestNodePacket>(payload);
                        Initialize(backtestJobModel);
                        break;
                    case PacketType.LiveNode:
                        var liveJobModel = JsonConvert.DeserializeObject<LiveNodePacket>(payload);
                        Initialize(liveJobModel);
                        break;
                    case PacketType.Debug:
                        var debugEventModel = JsonConvert.DeserializeObject<DebugPacket>(payload);
                        DisplayDebugPacket(debugEventModel);
                        break;
                    case PacketType.HandledError:
                        var handleErrorEventModel = JsonConvert.DeserializeObject<HandledErrorPacket>(payload);
                        DisplayHandledErrorPacket(handleErrorEventModel);
                        break;
                    case PacketType.BacktestResult:
                        var backtestResultEventModel = JsonConvert.DeserializeObject<BacktestResultPacket>(payload);
                        DisplayBacktestResultsPacket(backtestResultEventModel);
                        break;
                    case PacketType.RuntimeError:
                        var runtimeErrorEventModel = JsonConvert.DeserializeObject<RuntimeErrorPacket>(payload);
                        DisplayRuntimeErrorPacket(runtimeErrorEventModel);
                        break;
                    case PacketType.Log:
                        var logEventModel = JsonConvert.DeserializeObject<LogPacket>(payload);
                        DisplayLogPacket(logEventModel);
                        break;
                }
            }
        }

        /// <summary>
        /// This method is called when a new job is received.
        /// </summary>
        /// <param name="job">The job that is being executed</param>
        private void Initialize(AlgorithmNodePacket job)
        {
            _job = job;

            //Show warnings if the API token and UID aren't set.
            if (_job.UserId == 0)
            {
                _logging.Error("Your user id is not set. Please check your config.json file 'job-user-id' property.");
            }
            if (string.IsNullOrEmpty(job.Channel))
            {
                _logging.Error("Your API token is not set. Please check your config.json file 'api-access-token' property.");
            }

            _liveMode = job is LiveNodePacket;
            var url = GetUrl(job, _liveMode);

            Dispatcher.BeginInvoke((Action)(() => browser.Navigate(url)));
        }

        /// <summary>
        /// Displays the Backtest results packet
        /// </summary>
        /// <param name="packet">Backtest results</param>
        private void DisplayBacktestResultsPacket(BacktestResultPacket packet)
        {
            if (packet.Progress != 1) return;

            //Remove previous event handler:
            var url = GetUrl(_job, _liveMode, true);

            //Generate JSON:
            var jObj = new JObject();
            var dateFormat = "yyyy-MM-dd HH:mm:ss";
            dynamic final = jObj;
            final.dtPeriodStart = packet.PeriodStart.ToString(dateFormat, CultureInfo.InvariantCulture);
            final.dtPeriodFinished = packet.PeriodFinish.AddDays(1).ToString(dateFormat, CultureInfo.InvariantCulture);
            dynamic resultData = new JObject();
            resultData.version = 3;
            resultData.results = JObject.FromObject(packet.Results);
            resultData.statistics = JObject.FromObject(packet.Results.Statistics);
            resultData.iTradeableDates = 1;
            resultData.ranking = null;
            final.oResultData = resultData;
            var json = JsonConvert.SerializeObject(final);

            browser.LoadCompleted += (sender, args) =>
            {
                if (browser.Document == null)
                    return;
                browser.InvokeScript("eval", new object[] { "window.jnBacktest = JSON.parse('" + json + "');" });
                browser.InvokeScript("eval", new object[] { "$.holdReady(false)" });
            };
            Dispatcher.BeginInvoke((Action)(() => browser.Navigate(url)));

            foreach (var pair in packet.Results.Statistics)
            {
                _logging.Trace("STATISTICS:: " + pair.Key + " " + pair.Value);
            }
        }

        /// <summary>
        /// Display a handled error
        /// </summary>
        private void DisplayHandledErrorPacket(HandledErrorPacket packet)
        {
            var hstack = (!string.IsNullOrEmpty(packet.StackTrace) ? (Environment.NewLine + " " + packet.StackTrace) : string.Empty);
            _logging.Error(packet.Message + hstack);
        }

        /// <summary>
        /// Display a runtime error
        /// </summary>
        private void DisplayRuntimeErrorPacket(RuntimeErrorPacket packet)
        {
            var rstack = (!string.IsNullOrEmpty(packet.StackTrace) ? (Environment.NewLine + " " + packet.StackTrace) : string.Empty);
            _logging.Error(packet.Message + rstack);
        }

        /// <summary>
        /// Display a log packet
        /// </summary>
        private void DisplayLogPacket(LogPacket packet)
        {
            _logging.Trace(packet.Message);
        }

        /// <summary>
        /// Display a debug packet
        /// </summary>
        /// <param name="packet"></param>
        private void DisplayDebugPacket(DebugPacket packet)
        {
            _logging.Trace(packet.Message);
        }

        /// <summary>
        /// Get the URL for the embedded charting
        /// </summary>
        /// <param name="job">Job packet for the URL</param>
        /// <param name="liveMode">Is this a live mode chart?</param>
        /// <param name="holdReady">Hold the ready signal to inject data</param>
        private static string GetUrl(AlgorithmNodePacket job, bool liveMode = false, bool holdReady = false)
        {
            var hold = holdReady == false ? "0" : "1";
            var embedPage = liveMode ? "embeddedLive" : "embedded";

            string url = string.Format(
                CultureInfo.InvariantCulture,
                "https://www.quantconnect.com/terminal/{0}?user={1}&token={2}&pid={3}&version={4}&holdReady={5}&bid={6}",
                embedPage, job.UserId, job.Channel, job.ProjectId, Globals.Version, hold, job.AlgorithmId);

            return url;
        }

        /// <summary>
        /// Update the status label at the bottom of the form
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
//            StatisticsToolStripStatusLabel.Text = string.Concat("Performance: CPU: ", OS.CpuUsage.ToString("0.0"), "%", " Ram: ", OS.TotalPhysicalMemoryUsed, " Mb");

            if (_logging == null) return;

            while (_logging.Logs.TryDequeue(out LogEntry log))
            {
                switch (log.MessageType)
                {
                    case LogType.Debug:
//                        LogTextBox.AppendText(log.ToString(), Color.Black);
                        break;
                    default:
                    case LogType.Trace:
//                        LogTextBox.AppendText(log.ToString(), Color.Black);
                        break;
                    case LogType.Error:
//                        LogTextBox.AppendText(log.ToString(), Color.DarkRed);
                        break;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _logging.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}
