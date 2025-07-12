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
### Regression algorithm to test universe additions and removals with open positions
### </summary>
### <meta name="tag" content="regression test" />
class WeeklyUniverseSelectionRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_cash(100000)
        self.set_start_date(2013,10,1)
        self.set_end_date(2013,10,31)

        self.universe_settings.resolution = Resolution.HOUR

        # select IBM once a week, empty universe the other days
        self.add_universe("my-custom-universe", lambda dt: ["IBM"] if dt.day % 7 == 0 else [])

    def on_data(self, slice):
        if self.changes is None: return

        # liquidate removed securities
        for security in self.changes.removed_securities:
            if security.invested:
                self.log("{} Liquidate {}".format(self.time, security.symbol))
                self.liquidate(security.symbol)

        # we'll simply go long each security we added to the universe
        for security in self.changes.added_securities:
            if not security.invested:
                self.log("{} Buy {}".format(self.time, security.symbol))
                self.set_holdings(security.symbol, 1)

        self.changes = None

    def on_securities_changed(self, changes):
        self.changes = changes
