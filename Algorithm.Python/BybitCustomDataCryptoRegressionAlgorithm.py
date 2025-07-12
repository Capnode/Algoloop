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
import datetime

### <summary>
### Algorithm demonstrating and ensuring that Bybit crypto brokerage model works as expected with custom data types
### </summary>
class BybitCustomDataCryptoRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2022, 12, 13)
        self.set_end_date(2022, 12, 13)

        self.set_account_currency("USDT")
        self.set_cash(100000)

        self.set_brokerage_model(BrokerageName.BYBIT, AccountType.CASH)

        symbol = self.add_crypto("BTCUSDT").symbol
        self.btc_usdt = self.add_data(CustomCryptoData, symbol, Resolution.MINUTE).symbol

        # create two moving averages
        self.fast = self.ema(self.btc_usdt, 30, Resolution.MINUTE)
        self.slow = self.ema(self.btc_usdt, 60, Resolution.MINUTE)

    def on_data(self, data):
        if not self.slow.is_ready:
            return

        if self.fast.current.value > self.slow.current.value:
            if self.transactions.orders_count == 0:
                self.buy(self.btc_usdt, 1)
        else:
            if self.transactions.orders_count == 1:
                self.liquidate(self.btc_usdt)

    def on_order_event(self, order_event):
        self.debug(f"{self.time} {order_event}")

class CustomCryptoData(PythonData):
    def get_source(self, config, date, is_live_mode):
        tick_type_string = Extensions.tick_type_to_lower(config.tick_type)
        formatted_date = date.strftime("%Y%m%d")
        source = os.path.join(Globals.DataFolder, "crypto", "bybit", "minute",
                              config.symbol.value.lower(), f"{formatted_date}_{tick_type_string}.zip")

        return SubscriptionDataSource(source, SubscriptionTransportMedium.LOCAL_FILE, FileFormat.CSV)

    def reader(self, config, line, date, is_live_mode):
        csv = line.split(',')

        data = CustomCryptoData()
        data.symbol = config.symbol

        data_datetime = datetime.datetime.combine(date.date(), datetime.time()) + timedelta(milliseconds=int(csv[0]))
        data.time = Extensions.convert_to(data_datetime, config.data_time_zone, config.exchange_time_zone)
        data.end_time = data.time + timedelta(minutes=1)

        data["Open"] = float(csv[1])
        data["High"] = float(csv[2])
        data["Low"] = float(csv[3])
        data["Close"] = float(csv[4])
        data["Volume"] = float(csv[5])
        data.value = float(csv[4])

        return data
