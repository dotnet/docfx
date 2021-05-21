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
        private readonly JavaScriptEngine[] _engines = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new JavaScriptEngine[] { new JintJsEngine(new LocalPackage("data/javascript")), new ChakraCoreJsEngine(new LocalPackage("data/javascript")) }
            : new JavaScriptEngine[] { new JintJsEngine(new LocalPackage("data/javascript")) };

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
                Assert.ThrowsAny<Exception>(() => engine.Run("index.js", "main", inputJson));
            }
        }

        [Fact]
        public void SupportModuleExports()
        {
            foreach (var engine in _engines)
            {
                Assert.Equal("foo", engine.Run("module.js", "foo", JValue.CreateUndefined()));
                Assert.Equal("bar", engine.Run("module.js", "bar", JValue.CreateUndefined()));
            }
        }
    }
}
