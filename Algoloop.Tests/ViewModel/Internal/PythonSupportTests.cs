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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Algoloop.ViewModel.Internal;
using System.IO;

namespace Algoloop.Tests.ViewModel.Internal
{
    [TestClass()]
    public class PythonSupportTests
    {
        const string _infile = "infile";
        const string _outfile = "outfile";
        const string _runtimeConfigJson =
@"{
  ""runtimeOptions"": {
    ""tfm"": ""net5.0"",
    ""includedFrameworks"": [
      {
        ""name"": ""Microsoft.NETCore.App"",
        ""version"": ""5.0.15""
      }
    ],
    ""configProperties"": {
      ""System.GC.Server"": true,
      ""System.Reflection.Metadata.MetadataUpdater.IsSupported"": false
    }
  }
}";


        [TestMethod()]
        public void CopyRuntimeConfigTest()
        {
            // Arrange
            File.WriteAllText(_infile, _runtimeConfigJson);

            // Act
            PythonSupport.CopyRuntimeConfig(_infile, _outfile);

            // Assert
            string result = File.ReadAllText(_outfile);    
            Assert.IsTrue(_runtimeConfigJson.Length > 0);
        }
    }
}
