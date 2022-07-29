/*
 * Copyright 2022 Capnode AB
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

using Algoloop.ToolBox;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Logging;
using System.Diagnostics;
using System.IO;

namespace Algoloop.Tests.ToolBox
{
    [TestClass]
    public class MetastockConverterTests
    {
//        private const string SourceDir = "TestData";
        private const string SourceDir = "D:\\MSDATA";
        private const string DestDir = "Data";

        [TestInitialize]
        public void Initialize()
        {
            // Make Debug.Assert break execution
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new DefaultTraceListener());
            Log.LogHandler = new ConsoleLogHandler();

            // Set Globals
            Config.Set("data-directory", DestDir);
            Config.Set("data-folder", DestDir);
            Config.Set("cache-location", DestDir);
            Config.Set("version-id", string.Empty);
            Globals.Reset();

            // Remove Data folder
            if (Directory.Exists(DestDir))
            {
                Directory.Delete(DestDir, true);
            }
        }

        [TestMethod]
        public void MetastockConverter()
        {
            // Arrange
            string datafile = Path.Combine(
                DestDir,
                SecurityType.Equity.SecurityTypeToLower(),
                Market.Metastock,
                nameof(Resolution.Daily),
                "volvy.zip");
            string mapfile = Path.Combine(
                MapFile.GetMapFilePath(Market.Metastock, SecurityType.Equity),
                "volvy.csv");

            string[] args =
            {
                "--app=MetastockConverter",
                $"--source-dir={SourceDir}",
                $"--destination-dir={DestDir}",
            };

            // Act
            Program.Main(args);

            // Assert
            Assert.IsTrue(File.Exists(datafile));
            Assert.IsTrue(File.Exists(mapfile));
        }
    }
}
