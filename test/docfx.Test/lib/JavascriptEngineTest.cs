// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class JavascriptEngineTest
    {
        private readonly IJavaScriptEngine[] _engines = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new IJavaScriptEngine[] { new JintJsEngine("data/javascript"), new ChakraCoreJsEngine("data/javascript") }
            : new IJavaScriptEngine[] { new JintJsEngine("data/javascript") };

        [Theory]
        [InlineData("{'scalar':'hello','tags':[1,2.123],'page':{'value':3}}", "{'scalar':'hello','tags':[1,2.123],'page':{'value':3}}")]
        [InlineData("{'a':true}", "['a','b']")]
        public void RunJavascript(string input, string output)
        {
            var inputJson = JObject.Parse(input.Replace('\'', '"'));

            foreach (var engine in _engines)
            {
                var outputJson = engine.Run("index.js", "main", inputJson);
                Assert.Equal(output.Replace('\'', '"'), outputJson.ToString(Formatting.None));
            }
        }

        [Theory]
        [InlineData("{'error': true}")]
        public void RunJavascriptError(string input)
        {
            var inputJson = JObject.Parse(input.Replace('\'', '"'));

            foreach (var engine in _engines)
            {
                Assert.Throws<JavaScriptEngineException>(() => engine.Run("index.js", "main", inputJson));
            }
        }
    }
}
