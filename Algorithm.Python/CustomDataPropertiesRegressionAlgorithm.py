# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from AlgorithmImports import *

### <summary>
### Regression test to demonstrate setting custom Symbol Properties and Market Hours for a custom data import
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="importing data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="crypto" />
### <meta name="tag" content="regression test" />
class CustomDataPropertiesRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2011,9,13)   # Set Start Date
        self.set_end_date(2015,12,1)     # Set End Date
        self.set_cash(100000)           # Set Strategy Cash

        # Define our custom data properties and exchange hours
        self.ticker = 'BTC'
        properties = SymbolProperties("Bitcoin", "USD", 1, 0.01, 0.01, self.ticker)
        exchange_hours = SecurityExchangeHours.always_open(TimeZones.NEW_YORK)

        # Add the custom data to our algorithm with our custom properties and exchange hours
        self.bitcoin = self.add_data(Bitcoin, self.ticker, properties, exchange_hours, leverage=1, fill_forward=False)

        # Verify our symbol properties were changed and loaded into this security
        if self.bitcoin.symbol_properties != properties :
            raise Exception("Failed to set and retrieve custom SymbolProperties for BTC")

        # Verify our exchange hours were changed and loaded into this security
        if self.bitcoin.exchange.hours != exchange_hours :
            raise Exception("Failed to set and retrieve custom ExchangeHours for BTC")

        # For regression purposes on AddData overloads, this call is simply to ensure Lean can accept this
        # with default params and is not routed to a breaking function.
        self.add_data(Bitcoin, "BTCUSD")


    def on_data(self, data):
        if not self.portfolio.invested:
            if data['BTC'].close != 0 :
                self.order('BTC', self.portfolio.margin_remaining/abs(data['BTC'].close + 1))

    def on_end_of_algorithm(self):
        #Reset our Symbol property value, for testing purposes.
        self.symbol_properties_database.set_entry(Market.USA, self.market_hours_database.get_database_symbol_key(self.bitcoin.symbol), SecurityType.BASE,
            SymbolProperties.get_default("USD"))



class Bitcoin(PythonData):
    '''Custom Data Type: Bitcoin data from Quandl - http://www.quandl.com/help/api-for-bitcoin-data'''

    def get_source(self, config, date, is_live_mode):
        if is_live_mode:
            return SubscriptionDataSource("https://www.bitstamp.net/api/ticker/", SubscriptionTransportMedium.REST)

        #return "http://my-ftp-server.com/futures-data-" + date.to_string("Ymd") + ".zip"
        # OR simply return a fixed small data file. Large files will slow down your backtest
        return SubscriptionDataSource("https://www.quantconnect.com/api/v2/proxy/quandl/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc&api_key=WyAazVXnq7ATy_fefTqm", SubscriptionTransportMedium.REMOTE_FILE)


    def reader(self, config, line, date, is_live_mode):
        coin = Bitcoin()
        coin.symbol = config.symbol

        if is_live_mode:
            # Example Line Format:
            # {"high": "441.00", "last": "421.86", "timestamp": "1411606877", "bid": "421.96", "vwap": "428.58", "volume": "14120.40683975", "low": "418.83", "ask": "421.99"}
            try:
                live_btc = json.loads(line)

                # If value is zero, return None
                value = live_btc["last"]
                if value == 0: return None

                coin.time = datetime.now()
                coin.value = value
                coin["Open"] = float(live_btc["open"])
                coin["High"] = float(live_btc["high"])
                coin["Low"] = float(live_btc["low"])
                coin["Close"] = float(live_btc["last"])
                coin["Ask"] = float(live_btc["ask"])
                coin["Bid"] = float(live_btc["bid"])
                coin["VolumeBTC"] = float(live_btc["volume"])
                coin["WeightedPrice"] = float(live_btc["vwap"])
                return coin
            except ValueError:
                # Do nothing, possible error in json decoding
                return None

        # Example Line Format:
        # Date      Open   High    Low     Close   Volume (BTC)    Volume (Currency)   Weighted Price
        # 2011-09-13 5.8    6.0     5.65    5.97    58.37138238,    346.0973893944      5.929230648356
        if not (line.strip() and line[0].isdigit()): return None

        try:
            data = line.split(',')
            coin.time = datetime.strptime(data[0], "%Y-%m-%d")
            coin.end_time = coin.time + timedelta(days=1)
            coin.value = float(data[4])
            coin["Open"] = float(data[1])
            coin["High"] = float(data[2])
            coin["Low"] = float(data[3])
            coin["Close"] = float(data[4])
            coin["VolumeBTC"] = float(data[5])
            coin["VolumeUSD"] = float(data[6])
            coin["WeightedPrice"] = float(data[7])
            return coin

        except ValueError:
            # Do nothing, possible error in json decoding
            return None
