/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
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
 *
*/

using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Packets;

namespace QuantConnect.Tests.Common.Packets
{
    public class ControlsTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(FileAccess.Read)]
        [TestCase(FileAccess.Write)]
        [TestCase(FileAccess.ReadWrite)]
        public void StoragePermissionsJsonRoundTrip(FileAccess permissions)
        {
            var control = new Controls { StoragePermissions = permissions };
            var json = JsonConvert.SerializeObject(control);
            var result = JsonConvert.DeserializeObject<Controls>(json);

            Assert.AreEqual(permissions, result.StoragePermissions);
        }
    }
}
