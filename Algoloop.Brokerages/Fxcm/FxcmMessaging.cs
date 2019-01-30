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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Xml.Linq;
using fxcore2;
using System.Threading.Tasks;

namespace Algoloop.Brokerages.Fxcm
{
    class FxcmMessaging : IO2GSessionStatus, IO2GResponseListener
    {
        class PendingRequest
        {
            public string RequestId { get; set; }
            public object ResponseEvent { get; set; }
            public O2GResponse Response { get; set; }

            public PendingRequest(string requestId)
            {
                this.RequestId = requestId;
                this.ResponseEvent = new object();
            }
        }

        const string url = "www.fxcorporate.com/Hosts.jsp";
        const int MAX_BARS = 300;
        private O2GSession session;
        private O2GLoginRules loginRules;
        private O2GResponseReaderFactory readerFactory;
        private O2GRequestFactory requestFactory;
        private bool ticksLoaded = false;
        private bool positionsLoaded = false;
        private bool tradesLoaded = false;
        private bool hedgingAllowed = true;
        private List<PendingRequest> pendingRequests = new List<PendingRequest>();
        DateTime since = DateTime.MinValue;

        public FxcmMessaging()
        {
        }

        internal IDictionary<string, string> SystemProperties()
        {
            O2GResponse response = this.loginRules.getSystemPropertiesResponse();
            O2GSystemPropertiesReader systemResponseReader = this.readerFactory.createSystemPropertiesReader(response);
            return systemResponseReader.Properties;
        }

        //internal List<Trade> LoadReport(string url)
        //{
        //    // Get history trades
        //    XDocument xDoc;
        //    try
        //    {
        //        System.Net.WebClient webClient = new System.Net.WebClient();
        //        //        string content = webClient.DownloadString(url);
        //        //        System.IO.File.WriteAllText(System.IO.Path.Combine(STradebase.DataFolder, base.Id + ".xml"), content);
        //        xDoc = XDocument.Load(url);
        //        //        xDoc = XDocument.Parse(content);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(this + ".LoadReport() {0} - {1}", ex.GetType(), ex.Message);
        //        return null;
        //    }

        //    XNamespace rdf = "http://re.fxcm.com/2008/2.0/format-xml";
        //    //IEnumerable<XElement> xTrades = (from xNode in xDoc.Root.Element(rdf + "data").Elements(rdf + "table")
        //    //                                where (string)xNode.Attribute("name") == "closed_trades"
        //    //                                select xNode.Element(rdf+"body")).Elements(rdf+"row");

        //    List<Trade> trades = new List<Trade>();
        //    foreach (XElement xTable in xDoc.Root.Element(rdf + "data").Elements(rdf + "table"))
        //    {
        //        if ((string)xTable.Attribute("name") == "closed_trades")
        //        {
        //            foreach (XElement xRow in xTable.Element(rdf + "body").Elements(rdf + "row"))
        //            {
        //                Trade trade = new Trade(this);
        //                Debug.Assert(trade.Ticket == null, "trade.Ticket == null");
        //                string grouping = "";
        //                foreach (XElement xCell in xRow.Elements(rdf + "cell"))
        //                {
        //                    string name = xCell.Attribute("name").Value.ToString();
        //                    if (name.Equals("ticket_id"))
        //                        trade.Ticket = xCell.Value;
        //                    else if (name.Equals("symbol"))
        //                        trade.Instrument = this.InstrumentBySymbol(xCell.Value);
        //                    else if (name.Equals("quantity"))
        //                        trade.Size = (int)(double)xCell;
        //                    else if (name.Equals("open_date"))
        //                        trade.OpenTime = DateTime.Parse(xCell.Value);
        //                    else if (name.Equals("close_date"))
        //                        trade.CloseTime = DateTime.Parse(xCell.Value);
        //                    else if (name.Equals("comm"))
        //                        trade.Commission = (double)xCell;
        //                    else if (name.Equals("rollover"))
        //                        trade.Interest = (double)xCell;
        //                    else if (name.Equals("open_rate"))
        //                        trade.Open = (double)xCell;
        //                    else if (name.Equals("close_rate"))
        //                        trade.Close = (double)xCell;
        //                    else if (name.Equals("gross_pl"))
        //                        trade.Profit = (double)xCell;
        //                    else if (name.Equals("grouping_type"))
        //                        grouping = xCell.Value;
        //                }
        //                if (grouping == "2")
        //                    if (trade.OpenTime >= this.since)
        //                        trades.Add(trade);
        //                // Debug.WriteLine(this + " " + trade);
        //            }
        //        }
        //    }
        //    return trades;
        //}

        //internal void LoadTable(O2GTableType table)
        //{
        //    if (this.loginRules.isTableLoadedByDefault(table))
        //    {
        //        O2GResponse response = this.loginRules.getTableRefreshResponse(table);
        //        Debug.Assert(response != null, "response != null");
        //        this.onTablesUpdates(response);
        //    }
        //    else
        //    {
        //        O2GRequest request;
        //        if (table == O2GTableType.Accounts || table == O2GTableType.Messages)
        //            request = this.requestFactory.createRefreshTableRequest(table);
        //        else
        //        {
        //            request = this.requestFactory.createRefreshTableRequestByAccount(table, base.AccountId);
        //        }
        //        Debug.Assert(request != null, "request != null");
        //        this.session.sendRequest(request);
        //    }
        //}

        //bool ReadTableRow(O2GAccountRow row, Account account)
        //{
        //    if (row == null)
        //        return false;

        //    if (account.AccountId == null || account.AccountId == row.AccountID)
        //    {
        //        if (account.AccountId == null)
        //            account.AccountId = row.AccountID;
        //        Debug.Assert(account.AccountId.Equals(row.AccountID), "account.AccountId.Equals(row.AccountID)");

        //        account.Balance = row.Balance;
        //        account.Margin = row.UsedMargin;
        //        this.hedgingAllowed = row.MaintenanceType.Equals("Y");
        //    }
        //    return true;
        //}

        //bool ReadTableRow(O2GOfferRow row, Instrument instrument)
        //{
        //    if (row == null)
        //        return false;

        //    // Read once
        //    if (instrument.Symbol == null && row.isInstrumentValid)
        //    {
        //        instrument.Symbol = row.Instrument;
        //        //      Debug.Assert(instrument.Symbol.Equals(row.Instrument), "instrument.Symbol.Equals(row.Instrument)");

        //        if (row.isOfferIDValid)
        //            instrument.Id = row.OfferID;
        //        //      Debug.Assert(instrument.Id.Equals(row.OfferID), "instrument.Id.Equals(row.OfferID)");

        //        if (row.isContractCurrencyValid)
        //        {
        //            if (instrument.Symbol.Contains('/'))
        //            {
        //                int length = instrument.Symbol.Length;
        //                Debug.Assert(length > 3, "length > 3");
        //                instrument.Currency = instrument.Symbol.Substring(length - 3);
        //            }
        //            else
        //                instrument.Currency = row.ContractCurrency;
        //        }

        //        if (row.isContractMultiplierValid)
        //            instrument.Scale = row.ContractMultiplier;
        //        if (row.isDigitsValid)
        //            instrument.Digits = row.Digits;
        //        if (row.isSubscriptionStatusValid)
        //        {
        //            switch (row.SubscriptionStatus)
        //            {
        //                case "T": instrument.Status = InstrumentStatus.Open; break;
        //                case "V": instrument.Status = InstrumentStatus.Open; break;
        //                case "D": instrument.Status = InstrumentStatus.Closed; break;
        //                default: instrument.Status = InstrumentStatus.Closed; break;
        //            }
        //        }
        //    }

        //    if (row.isTimeValid)
        //        instrument.Time = row.Time;
        //    if (row.isAskValid)
        //        instrument.Ask = row.Ask;
        //    if (row.isBidValid)
        //        instrument.Bid = row.Bid;
        //    //      if (row.isVolumeValid)
        //    //        instrument.LastVolume = row.Volume;
        //    if (row.isBuyInterestValid)
        //        instrument.BuyInterest = row.BuyInterest;
        //    if (row.isSellInterestValid)
        //        instrument.SellInterest = row.SellInterest;

        //    return true;
        //}

        //bool ReadTableRow(O2GOrderRow row, Order order)
        //{
        //    if (row == null)
        //        return false;

        //    if (order.Ticket == null)
        //        order.Ticket = row.OrderID;
        //    Debug.Assert(order.Ticket.Equals(row.OrderID), "order.Ticket.Equals(row.OrderID)");

        //    if (order.Instrument == null)
        //        order.Instrument = this.InstrumentById(row.OfferID);
        //    Debug.Assert(order.Instrument.Equals(this.InstrumentById(row.OfferID)), "order.Instrument.Equals(this.InstrumentById(row.OfferID)");

        //    if (row.BuySell == "B")
        //        order.Size = row.Amount;
        //    else
        //        order.Size = -row.Amount;
        //    order.Price = row.Rate;
        //    //      order.StopLoss = row.Stop;
        //    //      order.Limit = row.Limit;
        //    order.Time = row.StatusTime;
        //    order.Comment = row.RequestTXT;
        //    order.Status = row.Status;

        //    return true;
        //}

        //bool ReadTableRow(O2GTradeRow row, Position position)
        //{
        //    if (row == null)
        //        return false;

        //    if (position.Ticket == null)
        //        position.Ticket = row.TradeID;
        //    Debug.Assert(position.Ticket.Equals(row.TradeID), "position.Ticket.Equals(row.TradeID)");

        //    if (position.Instrument == null)
        //        position.Instrument = this.InstrumentById(row.OfferID);
        //    Debug.Assert(position.Instrument.Equals(this.InstrumentById(row.OfferID)), "position.Instrument.Equals(this.InstrumentById(row.OfferID))");

        //    if (row.BuySell == "B")
        //        position.Size = row.Amount;
        //    else
        //        position.Size = -row.Amount;
        //    position.Open = row.OpenRate;
        //    //      position.Close = row.Close;
        //    //      position.StopLoss = row.Stop;
        //    //      position.Limit = row.Limit;
        //    //      position.Pips = row.PL;
        //    //      position.Balance = row.GrossPL;
        //    position.Commission = -row.Commission;
        //    position.Interest = row.RolloverInterest;
        //    position.OpenTime = row.OpenTime;
        //    position.Comment = row.OpenOrderRequestTXT;
        //    return true;
        //}

        //private bool ReadTableRow(O2GClosedTradeRow row, Trade trade)
        //{
        //    if (row == null)
        //        return false;

        //    if (trade.Ticket == null)
        //        trade.Ticket = row.TradeID;
        //    Debug.Assert(trade.Ticket.Equals(row.TradeID), "trade.Ticket.Equals(row.TradeID)");

        //    if (trade.Instrument == null)
        //        trade.Instrument = this.InstrumentById(row.OfferID);

        //    if (row.BuySell == "B")
        //    {
        //        trade.Size = row.Amount;
        //    }
        //    else
        //    {
        //        trade.Size = -row.Amount;
        //    }
        //    trade.OpenTime = row.OpenTime;
        //    trade.CloseTime = row.CloseTime;
        //    trade.Open = row.OpenRate;
        //    trade.Close = row.CloseRate;
        //    trade.Profit = row.GrossPL;

        //    trade.Commission = -row.Commission;
        //    trade.Interest = row.RolloverInterest;
        //    trade.Comment = row.OpenOrderRequestTXT;

        //    return (trade.OpenTime >= this.since);
        //}

        //private bool ReadMarketData(O2GMarketDataSnapshotResponseReader reader, int index, Bar bar)
        //{
        //    bar.Time = reader.getDate(index);
        //    bar.Open = reader.getBidOpen(index);
        //    bar.High = reader.getBidHigh(index);
        //    bar.Low = reader.getBidLow(index);
        //    bar.Close = reader.getBidClose(index);
        //    bar.Volume = reader.getVolume(index);
        //    return true;
        //}

        //private bool ReadMarketData(O2GMarketDataSnapshotResponseReader reader, int index, Instrument instrument)
        //{
        //    instrument.Time = reader.getDate(index);
        //    instrument.Bid = reader.getBid(index);
        //    instrument.Ask = reader.getAsk(index);
        //    return true;
        //}

        //public override async Task<bool> MainAsync()
        //{
        //    //      Debug.WriteLine(">{0}.Main()", this);
        //    bool main = await base.MainAsync();
        //    if (main)
        //    {
        //        try
        //        {
        //            // Connecting
        //            base.Status = OnlineStatus.Connecting;
        //            string user = base.Xconfig.Attribute("User").Value.ToString();
        //            string password = base.Xconfig.Attribute("Password").Value.ToString();
        //            string type = base.Xconfig.Attribute("Type").Value.ToString();
        //            switch (type)
        //            {
        //                case "Real": base.Kind = AccountState.Real; break;
        //                case "Demo": base.Kind = AccountState.Test; break;
        //                default: base.Kind = AccountState.Undefined; break;
        //            }

        //            XAttribute attribute = base.Xconfig.Attribute("Since");
        //            if (attribute != null)
        //                if (!DateTime.TryParse((string)attribute, out this.since))
        //                    Debug.WriteLine("{0}.Connect() Invalid format:{1}", this, attribute);

        //            this.session = O2GTransport.createSession();
        //            Debug.Assert(this.session != null, "this.session != null");
        //            this.session.subscribeSessionStatus(this);
        //            this.session.subscribeResponse(this);
        //            this.session.login(user, password, url, type);
        //            int timeout = 5 * 60 * 1000; // Timeout in ms
        //            while (timeout > 0 && (base.Status == OnlineStatus.Connecting || base.Status == OnlineStatus.Connected))
        //            {
        //                Thread.Sleep(50);
        //                timeout -= 50;
        //                if ((base.Status == OnlineStatus.Connected) && base.AccountId != null && this.ticksLoaded && this.positionsLoaded && this.tradesLoaded)
        //                {
        //                    base.CalcPositions();
        //                    base.Status = OnlineStatus.Active;
        //                }
        //            }

        //            if (base.Status.Equals(OnlineStatus.Active))
        //            {
        //                base.Cancel.Token.WaitHandle.WaitOne();
        //            }
        //            else if (timeout < 0)
        //                base.Status = OnlineStatus.Timeout;
        //            else
        //                base.Status = OnlineStatus.Rejected;
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine(this + ".Main() {0} - {1}", ex.GetType(), ex.Message);
        //            base.Status = OnlineStatus.Rejected;
        //        }

        //        if (this.session != null)
        //        {
        //            if (base.Status > OnlineStatus.Disconnecting)
        //            {
        //                this.session.logout();
        //                while (base.Status >= OnlineStatus.Disconnecting)
        //                    Thread.Sleep(50);
        //            }
        //            this.session.unsubscribeSessionStatus(this);
        //            this.session.unsubscribeResponse(this);
        //            this.session.Dispose();
        //        }
        //    }
        //    //      Debug.WriteLine("<{0}.Main()", this);
        //    return main;
        //}

        //public override bool Open(Order order)
        //{
        //    if (base.Open(order))
        //    {
        //        Debug.Assert(this.session != null, "this.session != null");
        //        Debug.Assert(base.Status >= OnlineStatus.Active, "base.Status >= StatusId.Active");
        //        Debug.Assert(order.Instrument != null, "order.Instrument != null");
        //        O2GValueMap valuemap = this.requestFactory.createValueMap();
        //        valuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
        //        valuemap.setString(O2GRequestParamsEnum.AccountID, base.AccountId);         // The identifier of the account the order should be placed for.
        //        valuemap.setString(O2GRequestParamsEnum.OfferID, order.Instrument.Id);             // The identifier of the instrument the order should be placed for.
        //        valuemap.setInt(O2GRequestParamsEnum.Amount, Math.Abs(order.Size));                  // The quantity of the instrument to be bought or sold.
        //        if (order.Price > 0)
        //        {
        //            valuemap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.LimitEntry);
        //            valuemap.setDouble(O2GRequestParamsEnum.Rate, order.Price);
        //        }
        //        else
        //            valuemap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.TrueMarketOpen);
        //        if (order.Size > 0)
        //            valuemap.setString(O2GRequestParamsEnum.BuySell, "B");             // The order direction: Constants.Sell for "Sell", Constants.Buy for "Buy".
        //        else
        //            valuemap.setString(O2GRequestParamsEnum.BuySell, "S");             // The order direction: Constants.Sell for "Sell", Constants.Buy for "Buy".
        //        if (order.Comment != null)
        //            valuemap.setString(O2GRequestParamsEnum.CustomID, order.Comment);   // The custom identifier of the order.

        //        O2GRequest request = this.requestFactory.createOrderRequest(valuemap);
        //        Debug.Assert(request != null, "request != null");
        //        this.session.sendRequest(request);
        //    }
        //    return true;
        //}

        //public override bool Update(Order order, double price, int size)
        //{
        //    if (base.Update(order, price, size))
        //    {
        //        Debug.Assert(this.session != null, "this.session != null");
        //        Debug.Assert(base.Status >= OnlineStatus.Active, "base.Status >= StatusId.Active");
        //        Debug.Assert(order.Instrument != null, "order.Instrument != null");
        //        O2GValueMap valuemap = this.requestFactory.createValueMap();
        //        valuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.EditOrder);
        //        valuemap.setString(O2GRequestParamsEnum.AccountID, base.AccountId);         // The identifier of the account
        //        valuemap.setString(O2GRequestParamsEnum.OrderID, order.Ticket);
        //        if (price > 0)
        //            valuemap.setDouble(O2GRequestParamsEnum.Rate, price);
        //        if (size > 0)
        //            valuemap.setInt(O2GRequestParamsEnum.Amount, size);
        //        O2GRequest request = this.requestFactory.createOrderRequest(valuemap);
        //        Debug.Assert(request != null, "request != null");
        //        this.session.sendRequest(request);
        //    }
        //    return true;
        //}

        //public override bool Close(Order order)
        //{
        //    if (base.Close(order))
        //    {
        //        Debug.Assert(this.session != null, "this.session != null");
        //        Debug.Assert(base.Status >= OnlineStatus.Active, "base.Status >= StatusId.Active");
        //        Debug.Assert(order.Instrument != null, "order.Instrument != null");
        //        O2GValueMap valuemap = this.requestFactory.createValueMap();
        //        valuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.DeleteOrder);
        //        valuemap.setString(O2GRequestParamsEnum.AccountID, base.AccountId);         // The identifier of the account .
        //        valuemap.setString(O2GRequestParamsEnum.OrderID, order.Ticket);
        //        O2GRequest request = this.requestFactory.createOrderRequest(valuemap);
        //        Debug.Assert(request != null, "request != null");
        //        this.session.sendRequest(request);
        //    }
        //    return true;
        //}

        //public override bool Close(Position position, double price, int size)
        //{
        //    if (base.Close(position))
        //    {
        //        Debug.Assert(this.session != null, "this.session != null");
        //        Debug.Assert(base.Status >= OnlineStatus.Active, "base.Status >= StatusId.Active");
        //        Debug.Assert(position.Instrument != null, "order.Instrument != null");
        //        O2GValueMap valuemap = this.requestFactory.createValueMap();
        //        valuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
        //        valuemap.setString(O2GRequestParamsEnum.AccountID, base.AccountId);         // The identifier of the account .
        //        valuemap.setString(O2GRequestParamsEnum.OfferID, position.Instrument.Id);             // The identifier of the instrument.
        //        valuemap.setInt(O2GRequestParamsEnum.Amount, Math.Abs(position.Size));                  // The quantity of the instrument to be bought or sold.
        //        if (position.Size > 0)
        //            valuemap.setString(O2GRequestParamsEnum.BuySell, "S");             // The order direction: Constants.Sell for "Sell", Constants.Buy for "Buy".
        //        else
        //            valuemap.setString(O2GRequestParamsEnum.BuySell, "B");             // The order direction: Constants.Sell for "Sell", Constants.Buy for "Buy".

        //        O2GPermissionChecker permissionChecker = this.loginRules.getPermissionChecker();
        //        if (permissionChecker.canCreateMarketCloseOrder(position.Instrument.Symbol) == O2GPermissionStatus.PermissionEnabled)
        //        {
        //            valuemap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.TrueMarketClose);
        //            valuemap.setString(O2GRequestParamsEnum.TradeID, position.Ticket);             // The indentifier of the trade.
        //        }
        //        else
        //            valuemap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.TrueMarketOpen);

        //        O2GRequest request = this.requestFactory.createOrderRequest(valuemap);
        //        Debug.Assert(request != null, "request != null");
        //        this.session.sendRequest(request);
        //    }
        //    return true;
        //}

        //public async override Task<Bars> GetBars(Instrument instrument, string period, DateTime startTime, DateTime endTime, Bars.PriceStream priceStream)
        //{
        //    //    Debug.WriteLine(">{0}.GetBackfill({1},{2},{3},{4})", this, instrument.Symbol, period, startTime, endTime);
        //    Bars bars = await base.GetBars(instrument, period, startTime, endTime, Bars.PriceStream.Bid);
        //    if (bars != null)
        //        return bars;

        //    Debug.Assert(this.requestFactory != null, "this.requestFactory != null");
        //    O2GTimeframeCollection timeFrames = this.requestFactory.Timeframes;
        //    Debug.Assert(timeFrames != null, "timeFrames != null");
        //    //      foreach (O2GTimeframe iTimeframe in timeFrames)
        //    //        Debug.WriteLine(iTimeframe.ID);
        //    O2GTimeframe timeFrame = timeFrames.FirstOrDefault<O2GTimeframe>(obj => obj.ID.Equals(period));
        //    if (timeFrame == null)
        //        timeFrame = timeFrames.Last();
        //    if (endTime > DateTime.Now)
        //        endTime = DateTime.Now;
        //    else
        //        endTime.AddTicks(1);
        //    O2GRequest marketDataRequest = this.requestFactory.createMarketDataSnapshotRequestInstrument(instrument.Symbol, timeFrame, MAX_BARS);
        //    Debug.Assert(marketDataRequest != null, "marketDataRequest != null");
        //    this.requestFactory.fillMarketDataSnapshotRequestTime(marketDataRequest, startTime, endTime, false);
        //    Debug.Assert(marketDataRequest != null, "marketDataRequest != null");
        //    this.session.sendRequest(marketDataRequest);
        //    bars = new Bars();
        //    DateTime first = DateTime.MaxValue;
        //    O2GResponse response = this.WaitResponse(marketDataRequest.RequestID);
        //    while (response != null)
        //    {
        //        Debug.Assert(response.Type.Equals(O2GResponseType.MarketDataSnapshot), "response.Type.Equals(O2GResponseType.MarketDataSnapshot)");
        //        O2GResponseReaderFactory factory = this.session.getResponseReaderFactory();
        //        O2GMarketDataSnapshotResponseReader reader = factory.createMarketDataSnapshotReader(response);
        //        int count = reader.Count;
        //        int added = 0;
        //        if (count == 0)
        //            break;
        //        if (reader.isBar)
        //        {
        //            for (int i = 0; i < count; i++)
        //            {
        //                Bar bar = new Bar();
        //                this.ReadMarketData(reader, i, bar);
        //                if (bar.Time < first)
        //                {
        //                    bars.Add(bar);
        //                    added++;
        //                }
        //            }
        //            bars.Sort();
        //            first = bars.First().Time;
        //        }
        //        response = null;
        //        if (added > 0 && startTime > DateTime.MinValue && first > startTime)
        //        {
        //            //          Debug.WriteLine("{0}.GetBackfill({1},{2},{3})",this,symbol,startTime,first);
        //            this.requestFactory.fillMarketDataSnapshotRequestTime(marketDataRequest, startTime, first, false);
        //            this.session.sendRequest(marketDataRequest);
        //            response = this.WaitResponse(marketDataRequest.RequestID);
        //        }
        //    }
        //    DateTime from = DateTime.MinValue;
        //    DateTime to = DateTime.MinValue;
        //    if (bars.Count > 0)
        //    {
        //        from = bars.First().Time;
        //        to = bars.Last().Time;
        //    }
        //    Debug.WriteLine("<{0}.GetBackfill() count={1} from={2} to={3}", this, bars.Count, from, to);
        //    return bars;
        //}

        public void onSessionStatusChanged(O2GSessionStatusCode status)
        {
        //    switch (status)
        //    {
        //        case O2GSessionStatusCode.Connecting:
        //            //          Debug.WriteLine(this+".onSessionStatusChanged({0})",status);
        //            base.Status = OnlineStatus.Connecting;
        //            break;

        //        case O2GSessionStatusCode.Connected:
        //            //          Debug.WriteLine("{0}.onSessionStatusChanged({1})",this,status);
        //            {
        //                this.loginRules = this.session.getLoginRules();
        //                if (this.loginRules != null)
        //                {
        //                    base.Status = OnlineStatus.Connected;
        //                    this.readerFactory = this.session.getResponseReaderFactory();
        //                    Debug.Assert(this.readerFactory != null, "this.readerFactory != null");
        //                    this.requestFactory = this.session.getRequestFactory();
        //                    Debug.Assert(this.requestFactory != null, "this.requestFactory != null");
        //                    foreach (O2GTimeframe timeframe in this.requestFactory.Timeframes)
        //                        base.Timeframes.Add(timeframe.ID);

        //                    LoadTable(O2GTableType.Accounts);
        //                    //            LoadTable(O2GTableType.Summary);
        //                    LoadTable(O2GTableType.Offers);
        //                    LoadTable(O2GTableType.Orders);
        //                    LoadTable(O2GTableType.Trades);
        //                    LoadTable(O2GTableType.ClosedTrades);
        //                    //            LoadTable(O2GTableType.Messages);

        //                    IDictionary<string, string> properties = this.SystemProperties();
        //                    base.Currency = properties["BASE_CRNCY"];
        //                    //            base.TradingAllowed = properties["MARKET_OPEN"] == "Y";
        //                }
        //            }
        //            break;

        //        case O2GSessionStatusCode.Disconnecting:
        //            //          Debug.WriteLine(this+".onSessionStatusChanged({0})",status);
        //            base.Status = OnlineStatus.Disconnecting;
        //            break;

        //        case O2GSessionStatusCode.Disconnected:
        //            //          Debug.WriteLine(this + ".onSessionStatusChanged({0})", status);
        //            this.requestFactory = null;
        //            this.readerFactory = null;
        //            this.loginRules = null;
        //            this.ticksLoaded = false;
        //            this.positionsLoaded = false;
        //            this.tradesLoaded = false;
        //            base.Status = OnlineStatus.Disabled;
        //            break;

        //        case O2GSessionStatusCode.TradingSessionRequested:
        //            Debug.WriteLine(this + ".onSessionStatusChanged({0})", status);
        //            foreach (O2GSessionDescriptor desc in this.session.getTradingSessionDescriptors())
        //                Debug.WriteLine(desc.ToString());
        //            break;

        //        default:
        //            Debug.WriteLine(this + ".onSessionStatusChanged({0})", status);
        //            break;
        //    }
        }

        public void onLoginFailed(string error)
        {
        //    Debug.WriteLine(this + ".onLoginFailed(): " + error);
        //    base.Status = OnlineStatus.Rejected;
        }

        private O2GResponse WaitResponse(string requestId)
        {
            PendingRequest request = new PendingRequest(requestId);
            this.pendingRequests.Add(request);
            lock (request.ResponseEvent)
                Monitor.Wait(request.ResponseEvent, 60000);
            bool ok = this.pendingRequests.Remove(request);
            Debug.Assert(ok, "ok");
            return request.Response;
        }

        public void onRequestCompleted(string requestId, O2GResponse response)
        {
        //    //      Debug.WriteLine(this + ".onRequestCompleted(response={0})", response.Type);
        //    if (!this.ProcessResponse(response))
        //    {
        //        PendingRequest request = this.pendingRequests.Find(req => req.RequestId.Equals(requestId));
        //        if (request != null)
        //        {
        //            request.Response = response;
        //            lock (request.ResponseEvent)
        //                Monitor.PulseAll(request.ResponseEvent);
        //        }
        //    }
        }

        public void onRequestFailed(string requestId, string error)
        {
            Debug.WriteLine(this + ".onRequestFailed() " + error);
            PendingRequest request = this.pendingRequests.Find(req => req.RequestId.Equals(requestId));
            if (request != null)
            {
                lock (request.ResponseEvent)
                    Monitor.PulseAll(request.ResponseEvent);
            }
        }

        public void onTablesUpdates(O2GResponse response)
        {
        //    //      Debug.WriteLine(this + ".onTablesUpdates({0})", response.Type);
        //    if (base.Status >= OnlineStatus.Connected)
        //    {
        //        this.ProcessResponse(response);
        //    }
        }

        //private bool ProcessResponse(O2GResponse response)
        //{
        //    switch (response.Type)
        //    {
        //        case O2GResponseType.TablesUpdates:
        //            DoTableUpdates(response);
        //            if (base.CalcPositions())
        //                base.OnUpdate(this);
        //            break;

        //        case O2GResponseType.GetAccounts:
        //            DoGetAccounts(response);
        //            break;

        //        case O2GResponseType.GetOffers:
        //            DoGetOffers(response);
        //            if (base.CalcPositions())
        //                base.OnUpdate(this);
        //            break;

        //        case O2GResponseType.GetOrders:
        //            DoGetOrders(response);
        //            break;

        //        case O2GResponseType.GetTrades:
        //            DoGetTrades(response);
        //            break;

        //        case O2GResponseType.GetClosedTrades:
        //            DoGetClosedTrades(response);
        //            break;

        //        default:
        //            Debug.WriteLine($"Unknown response: {response.Type}");
        //            return false;
        //    }

        //    return true;
        //}

        //private void DoGetAccounts(O2GResponse response)
        //{
        //    Debug.Assert(response.Type == O2GResponseType.GetAccounts, "response.Type == O2GResponseType.GetAccounts");
        //    O2GAccountsTableResponseReader reader = this.readerFactory.createAccountsTableReader(response);
        //    int count = reader.Count;
        //    //      Debug.WriteLine(this + ".UpdateTables({0}) count={1}", response.Type, count);
        //    for (int i = 0; i < count; i++)
        //    {
        //        O2GAccountRow row = reader.getRow(i);
        //        bool ok = ReadTableRow(row, this);
        //        Debug.Assert(ok, "ok");
        //        //        Debug.WriteLine(this);
        //    }
        //}

        //private void DoGetOffers(O2GResponse response)
        //{
        //    Debug.Assert(response.Type == O2GResponseType.GetOffers, "response.Type == O2GResponseType.GetOffers");

        //    // Get account row
        //    O2GAccountRow accountRow = null;
        //    Debug.Assert(this.loginRules != null, "this.loginRules != null");
        //    O2GTradingSettingsProvider tradingSettingsProvider = this.loginRules.getTradingSettingsProvider();
        //    Debug.Assert(this.loginRules.isTableLoadedByDefault(O2GTableType.Accounts), "this.loginRules.isTableLoadedByDefault(O2GTableType.Accounts)");
        //    O2GResponse accountsResponse = this.loginRules.getTableRefreshResponse(O2GTableType.Accounts);
        //    O2GAccountsTableResponseReader accountsReader = this.readerFactory.createAccountsTableReader(accountsResponse);
        //    for (int ai = 0; ai < accountsReader.Count; ai++)
        //    {
        //        O2GAccountRow row = accountsReader.getRow(ai);
        //        if (base.AccountId.Equals(row.AccountID))
        //        {
        //            accountRow = row;
        //            break;
        //        }
        //    }

        //    // Get offers              
        //    Instruments instruments = base.InstrumentsList.FirstOrDefault();
        //    Debug.Assert(instruments != null);
        //    instruments.Clear();
        //    O2GOffersTableResponseReader reader = this.readerFactory.createOffersTableReader(response);
        //    int count = reader.Count;
        //    bool ok;
        //    bool update = false;

        //    //      Debug.WriteLine(this + ".UpdateTables({0}) count={1}", response.Type, count);
        //    for (int i = 0; i < count; i++)
        //    {
        //        O2GOfferRow row = reader.getRow(i);
        //        Instrument instrument = new Instrument(this);
        //        ok = ReadTableRow(row, instrument);
        //        Debug.Assert(ok, "ok");
        //        //             Debug.WriteLine(this + " new " + instrument);

        //        // Get symbol extra data
        //        instrument.Basesize = tradingSettingsProvider.getBaseUnitSize(row.Instrument, accountRow);
        //        instrument.Minsize = tradingSettingsProvider.getMinQuantity(row.Instrument, accountRow);
        //        instrument.Maxsize = tradingSettingsProvider.getMaxQuantity(row.Instrument, accountRow);
        //        instrument.Margin = tradingSettingsProvider.getMMR(row.Instrument, accountRow);

        //        // Subscribe on symbol data if subscription disabled (valid after next login)
        //        O2GPermissionChecker permissionChecker = this.loginRules.getPermissionChecker();
        //        Debug.Assert(permissionChecker != null, "permissionChecker != null");
        //        O2GPermissionStatus permission = permissionChecker.canChangeOfferSubscription(row.Instrument);
        //        if (permission.Equals(O2GPermissionStatus.PermissionEnabled)
        //            && (row.SubscriptionStatus.Equals("D") || row.SubscriptionStatus.Equals("V")))
        //        {
        //            O2GValueMap valueMap = this.requestFactory.createValueMap();
        //            valueMap.setString(O2GRequestParamsEnum.Command, Constants.Commands.SetSubscriptionStatus);
        //            valueMap.setString(O2GRequestParamsEnum.SubscriptionStatus, "T");
        //            valueMap.setString(O2GRequestParamsEnum.OfferID, row.OfferID);
        //            O2GRequest request = this.requestFactory.createOrderRequest(valueMap);
        //            this.session.sendRequest(request);
        //            update = true;
        //        }

        //        instruments.Add(instrument);
        //    }

        //    this.ticksLoaded = true;
        //    if (update)
        //        Trace.TraceInformation(this + " Subscription is updated, please logout and login again");
        //}

        //private void DoGetOrders(O2GResponse response)
        //{
        //    Debug.Assert(response.Type == O2GResponseType.GetOrders, "response.Type == O2GResponseType.GetOrders");
        //    O2GOrdersTableResponseReader reader = this.readerFactory.createOrdersTableReader(response);
        //    int count = reader.Count;
        //    //      Debug.WriteLine(this + ".UpdateTables({0}) count={1}", response.Type, count);
        //    bool ok;
        //    List<Order> orders = new List<Order>();
        //    for (int i = 0; i < count; i++)
        //    {
        //        O2GOrderRow row = reader.getRow(i);
        //        Order order = new Order(base.ConfigurationService);
        //        ok = ReadTableRow(row, order);
        //        Debug.Assert(ok, "ok");
        //        //             Debug.WriteLine(this + " " + order);
        //        orders.Add(order);
        //    }
        //    orders.Sort();
        //    base.Orders.Clear();
        //    base.Orders.AddItems(orders);
        //}

        //private void DoGetTrades(O2GResponse response)
        //{
        //    Debug.Assert(response.Type == O2GResponseType.GetTrades, "response.Type == O2GResponseType.GetTrades");
        //    O2GTradesTableResponseReader reader = this.readerFactory.createTradesTableReader(response);
        //    int count = reader.Count;
        //    bool ok;
        //    List<Position> positions = new List<Position>();
        //    //      Debug.WriteLine(this + ".UpdateTables({0}) count={1}", response.Type, count);
        //    for (int i = 0; i < count; i++)
        //    {
        //        O2GTradeRow row = reader.getRow(i);
        //        Position position = new Position(base.ConfigurationService);
        //        ok = ReadTableRow(row, position);
        //        Debug.Assert(ok, "ok");
        //        //              Debug.WriteLine(this + " " + position);
        //        positions.Add(position);
        //    }
        //    positions.Sort();
        //    base.Positions.Clear();
        //    base.Positions.AddItems(positions);
        //    this.positionsLoaded = true;
        //}

        //private void DoGetClosedTrades(O2GResponse response)
        //{
        //    Debug.Assert(response.Type == O2GResponseType.GetClosedTrades, "response.Type == O2GResponseType.GetClosedTrades");
        //    Debug.Assert(this.loginRules.isTableLoadedByDefault(O2GTableType.Accounts), "this.loginRules.isTableLoadedByDefault(O2GTableType.Accounts)");
        //    O2GResponse accountsResponse = this.loginRules.getTableRefreshResponse(O2GTableType.Accounts);
        //    O2GAccountsTableResponseReader accountsReader = this.readerFactory.createAccountsTableReader(accountsResponse);
        //    List<Trade> trades = null;
        //    for (int i = 0; i < accountsReader.Count; i++)
        //    {
        //        O2GAccountRow row = accountsReader.getRow(i);
        //        if (base.AccountId.Equals(row.AccountID))
        //        {
        //            string url = session.getReportURL(row, this.since, DateTime.FromOADate(0), "xml", "REPORT_NAME_CUSTOMER_ACCOUNT_STATEMENT", "enu", 0);
        //            trades = this.LoadReport(url);
        //        }
        //    }
        //    if (trades == null)
        //        return;

        //    O2GClosedTradesTableResponseReader reader = this.readerFactory.createClosedTradesTableReader(response);
        //    int count = reader.Count;
        //    bool ok;
        //    //      Debug.WriteLine(this + ".UpdateTables({0}) count={1}(+{2})", response.Type, count, base.Trades.Count);
        //    for (int i = 0; i < count; i++)
        //    {
        //        O2GClosedTradeRow row = reader.getRow(i);
        //        Trade trade = new Trade(this);
        //        ok = ReadTableRow(row, trade);
        //        if (ok)
        //        {
        //            //              Debug.WriteLine(this + " " + trade);
        //            if (!trades.Exists(t => t.Ticket.Equals(trade.Ticket))
        //                && trade.OpenTime >= this.since)
        //                trades.Add(trade);
        //        }
        //    }
        //    trades.Sort();
        //    base.Trades.Clear();
        //    base.Trades.AddItems(trades);
        //    this.tradesLoaded = true;
        //}

        //private void DoTableUpdates(O2GResponse response)
        //{
        //    Debug.Assert(response.Type == O2GResponseType.TablesUpdates, "response.Type == O2GResponseType.TablesUpdates");
        //    O2GTablesUpdatesReader reader = this.readerFactory.createTablesUpdatesReader(response);
        //    int count = reader.Count;
        //    for (int i = 0; i < count; i++)
        //    {
        //        O2GTableType table = reader.getUpdateTable(i);
        //        O2GTableUpdateType type = reader.getUpdateType(i);
        //        bool ok;
        //        switch (table)
        //        {
        //            case O2GTableType.Accounts:
        //                {
        //                    //              Debug.WriteLine(this + ".UpdateTables({0}) table={1} type={2}", response.Type, table, type);
        //                    O2GAccountRow row = reader.getAccountRow(i);
        //                    switch (type)
        //                    {
        //                        case O2GTableUpdateType.Insert:
        //                            ok = ReadTableRow(row, this);
        //                            Debug.Assert(ok, "ok1");
        //                            base.OnAdd(this);
        //                            //                  Debug.WriteLine(this);
        //                            break;

        //                        case O2GTableUpdateType.Delete:
        //                            base.OnRemove(this);
        //                            break;

        //                        case O2GTableUpdateType.Update:
        //                            ok = ReadTableRow(row, this);
        //                            Debug.Assert(ok, "ok2");
        //                            base.OnUpdate(this);
        //                            //                  Debug.WriteLine(this);
        //                            break;

        //                        default:
        //                            throw new NotImplementedException();
        //                    }
        //                }
        //                break;

        //            case O2GTableType.Summary:
        //                Debug.WriteLine(this + ".UpdateTables({0}) table={1} type={2}", response.Type, table, type);
        //                throw new NotImplementedException();

        //            case O2GTableType.Offers:
        //                {
        //                    //                Debug.WriteLine(this + ".UpdateTables({0}) table={1} type={2}", response.Type, table, type);
        //                    O2GOfferRow row = reader.getOfferRow(i);
        //                    string symbol = row.Instrument;
        //                    if (symbol.Equals(string.Empty))
        //                        break;
        //                    Instrument instrument;
        //                    switch (type)
        //                    {
        //                        case O2GTableUpdateType.Insert:
        //                            instrument = new Instrument(this);
        //                            ok = ReadTableRow(row, instrument);
        //                            Debug.Assert(ok, "ok3");
        //                            base.OnAdd(instrument);
        //                            break;

        //                        case O2GTableUpdateType.Delete:
        //                            instrument = base.InstrumentBySymbol(symbol);
        //                            Debug.Assert(instrument != null, "Delete instrument != null:" + symbol);
        //                            base.OnRemove(instrument);
        //                            break;

        //                        case O2GTableUpdateType.Update:
        //                            instrument = base.InstrumentBySymbol(symbol);
        //                            Debug.Assert(instrument != null, "Update instrument != null:" + symbol);
        //                            ok = ReadTableRow(row, instrument);
        //                            Debug.Assert(ok, "ok4");
        //                            base.OnUpdate(instrument);
        //                            break;

        //                        default:
        //                            throw new NotImplementedException();
        //                    }
        //                }
        //                break;

        //            case O2GTableType.Orders:
        //                {
        //                    //              Debug.WriteLine(this + ".UpdateTables({0}) table={1} type={2}", response.Type, table, type);
        //                    O2GOrderRow row = reader.getOrderRow(i);
        //                    Order order;
        //                    switch (type)
        //                    {
        //                        case O2GTableUpdateType.Insert:
        //                            order = new Order(base.ConfigurationService);
        //                            ok = ReadTableRow(row, order);
        //                            Debug.Assert(ok, "ok5");
        //                            base.OnAdd(order);
        //                            break;

        //                        case O2GTableUpdateType.Delete:
        //                            order = base.Orders.FirstOrDefault<Order>(obj => obj.Ticket.Equals(row.OrderID));
        //                            if (order == null)
        //                            {
        //                                Debug.WriteLine(this + "*** Delete order == null");
        //                                order = new Order(base.ConfigurationService);
        //                                ok = ReadTableRow(row, order);
        //                                base.OnAdd(order);
        //                            }
        //                            base.OnRemove(order);
        //                            break;

        //                        case O2GTableUpdateType.Update:
        //                            order = base.Orders.FirstOrDefault<Order>(obj => obj.Ticket.Equals(row.OrderID));
        //                            if (order == null)
        //                            {
        //                                Debug.WriteLine(this + "*** Update order == null");
        //                                order = new Order(base.ConfigurationService);
        //                                ok = ReadTableRow(row, order);
        //                                base.OnAdd(order);
        //                            }
        //                            ok = ReadTableRow(row, order);
        //                            Debug.Assert(ok, "ok6");
        //                            base.OnUpdate(order);
        //                            break;

        //                        default:
        //                            throw new NotImplementedException();
        //                    }
        //                }
        //                break;

        //            case O2GTableType.Trades:
        //                {
        //                    //              Debug.WriteLine(this + ".UpdateTables({0}) table={1} type={2}", response.Type, table, type);
        //                    O2GTradeRow row = reader.getTradeRow(i);
        //                    Position position;
        //                    switch (type)
        //                    {
        //                        case O2GTableUpdateType.Insert:
        //                            position = new Position(base.ConfigurationService);
        //                            ok = ReadTableRow(row, position);
        //                            Debug.Assert(ok, "ok7");
        //                            base.OnAdd(position);
        //                            break;

        //                        case O2GTableUpdateType.Delete:
        //                            position = base.Positions.FirstOrDefault<Position>(obj => obj.Ticket.Equals(row.TradeID));
        //                            Debug.Assert(position != null, "Delete position != null");
        //                            base.OnRemove(position);
        //                            break;

        //                        case O2GTableUpdateType.Update:
        //                            position = base.Positions.FirstOrDefault<Position>(obj => obj.Ticket.Equals(row.TradeID));
        //                            if (position == null)
        //                                Debug.WriteLine(this + ".DoTableUpdates() ticket {0} not found", row.TradeID);
        //                            else
        //                            {
        //                                ok = ReadTableRow(row, position);
        //                                Debug.Assert(ok, "ok8");
        //                                base.OnUpdate(position);
        //                            }
        //                            break;
        //                    }
        //                }
        //                break;

        //            case O2GTableType.ClosedTrades:
        //                {
        //                    //              Debug.WriteLine(this + ".UpdateTables({0}) table={1} type={2}", response.Type, table, type);
        //                    O2GClosedTradeRow row = reader.getClosedTradeRow(i);
        //                    Trade trade;

        //                    switch (type)
        //                    {
        //                        case O2GTableUpdateType.Insert:
        //                            trade = new Trade(this);
        //                            if (ReadTableRow(row, trade))
        //                                base.OnAdd(trade);
        //                            break;

        //                        case O2GTableUpdateType.Delete:
        //                            trade = base.Trades.FirstOrDefault<Trade>(obj => obj.Ticket.Equals(row.TradeID));
        //                            Debug.Assert(trade != null, "Delete trade != null");
        //                            base.OnRemove(trade);
        //                            break;

        //                        case O2GTableUpdateType.Update:
        //                            trade = base.Trades.FirstOrDefault<Trade>(obj => obj.Ticket.Equals(row.TradeID));
        //                            Debug.Assert(trade != null, "Update trade != null");
        //                            if (ReadTableRow(row, trade))
        //                                base.OnUpdate(trade);
        //                            break;
        //                    }
        //                }
        //                break;

        //            case O2GTableType.Messages:
        //                {
        //                    //              Debug.WriteLine(this + ".UpdateTables({0}) table={1} type={2}", response.Type, table, type);
        //                    O2GMessageRow row = reader.getMessageRow(i);
        //                    switch (type)
        //                    {
        //                        case O2GTableUpdateType.Insert:
        //                            Debug.WriteLine(this + " Message: From=" + row.From + " Subject=" + row.Subject + " Text=" + row.Text);
        //                            break;

        //                        case O2GTableUpdateType.Delete:
        //                            throw new NotImplementedException();

        //                        case O2GTableUpdateType.Update:
        //                            throw new NotImplementedException();

        //                        default:
        //                            throw new NotImplementedException();
        //                    }
        //                }
        //                break;
        //        }
        //    }
        //}
    }
}
